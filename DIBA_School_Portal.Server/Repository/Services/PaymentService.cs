using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class PaymentService : IPaymentService
    {
        private const string DefaultFlutterwaveBaseUrl = "https://api.flutterwave.com/v3";
        private const string DefaultFlutterwaveCurrency = "NGN";

        private readonly AppDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IAuditLog _auditLog;
        private readonly INotificationLog _notification;
        private readonly IHttpContextAccessor _http;

        public PaymentService(
            AppDbContext context,
            ILogger<PaymentService> logger,
            HttpClient httpClient,
            IConfiguration configuration,
            IAuditLog auditLog,
            INotificationLog notification,
            IHttpContextAccessor http)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
            _notification = notification ?? throw new ArgumentNullException(nameof(notification));
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        private string CurrentUserId =>
            _http.HttpContext?.User?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "System";

        private string? FlutterwaveSecretKey =>
            _configuration["Flutterwave:SecretKey"];

        private string FlutterwaveBaseUrl =>
            _configuration["Flutterwave:BaseUrl"] ?? DefaultFlutterwaveBaseUrl;

        private string FlutterwaveCurrency =>
            _configuration["Flutterwave:Currency"] ?? DefaultFlutterwaveCurrency;

        private string FlutterwaveRedirectUrl =>
            _configuration["Flutterwave:RedirectUrl"]
            ?? "https://localhost:12050/student/payment/callback";

        // ==========================================================
        // BILL BREAKDOWN — unchanged
        // ==========================================================
        #region GetStudentBillBreakdownAsync
        public async Task<ServiceResponse<StudentBillResponseDto>> GetStudentBillBreakdownAsync(
            Guid studentProfileId, Guid termId)
        {
            var response = new ServiceResponse<StudentBillResponseDto>();
            try
            {
                var student = await _context.Students
                    .Include(s => s.SchoolClass)
                    .FirstOrDefaultAsync(s => s.Id == studentProfileId);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var term = await _context.SchoolTerms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == termId);

                if (term == null)
                {
                    response.Success = false;
                    response.Message = "Term not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var previousDebt = await CalculatePreviousOutstandingDebtAsync(
                    studentProfileId, term.StartDate);

                var classFeePrices = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .Where(cfp => cfp.SchoolClassId == student.SchoolClassId
                               && cfp.Fee.TermId == termId)
                    .AsNoTracking()
                    .ToListAsync();

                var successfulPayments = await _context.StudentPayments
                    .Include(p => p.PaymentItems)
                    .Where(p => p.StudentProfileId == studentProfileId
                             && p.TermId == termId
                             && p.Status == PaymentStatus.Successful)
                    .AsNoTracking()
                    .ToListAsync();

                var feeBreakdowns = new List<FeeItemBreakdownDto>();
                decimal currentTermBilled = 0;

                foreach (var cfp in classFeePrices)
                {
                    var itemAmount = cfp.Amount;
                    currentTermBilled += itemAmount;

                    var paidTowardThis = successfulPayments
                        .SelectMany(p => p.PaymentItems)
                        .Where(pi => pi.FeeId == cfp.FeeId)
                        .Sum(pi => pi.AmountAllocated);

                    feeBreakdowns.Add(new FeeItemBreakdownDto
                    {
                        FeeId = cfp.FeeId,
                        FeeName = cfp.Fee.FeeName,
                        Description = cfp.Fee.Description,
                        Amount = itemAmount,
                        MinimumAmountToPay = cfp.MinimumAmountToPay,
                        AmountPaidTowardThis = paidTowardThis,
                        IsFullyPaid = paidTowardThis >= itemAmount,
                    });
                }

                decimal totalBilled = currentTermBilled + previousDebt;
                decimal totalPaidSoFar = successfulPayments.Sum(p => p.AmountPaid);
                decimal balanceRemaining = Math.Max(0, totalBilled - totalPaidSoFar);

                decimal totalMinimumRequired = feeBreakdowns
                    .Where(f => !f.IsFullyPaid)
                    .Sum(f => Math.Max(0, f.MinimumAmountToPay - f.AmountPaidTowardThis));

                var paymentHistory = successfulPayments.Select(p => new PaymentHistoryDto
                {
                    PaymentId = p.Id,
                    ReferenceNumber = p.ReferenceNumber,
                    AmountPaid = p.AmountPaid,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate,
                }).ToList();

                response.Data = new StudentBillResponseDto
                {
                    StudentProfileId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    ClassName = student.SchoolClass?.Name ?? "—",
                    TermId = termId,
                    AcademicSessionId = term.AcademicSessionId,
                    TotalBilledAmount = totalBilled,
                    TotalPaidSoFar = totalPaidSoFar,
                    TotalBalanceRemaining = balanceRemaining,
                    MinimumAmountRequired = totalMinimumRequired,
                    FeeItems = feeBreakdowns,
                    PaymentHistory = paymentHistory,
                };
                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student bill breakdown");
                response.Success = false;
                response.Message = $"Error retrieving student bill: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // INITIALIZE PAYMENT — reject if no fees for the term
        // ==========================================================
        #region RecordPaymentAsync
        public async Task<ServiceResponse<string>> RecordPaymentAsync(CreatePaymentDto paymentDto)
        {
            var response = new ServiceResponse<string>();
            try
            {
                if (paymentDto == null)
                {
                    response.Success = false;
                    response.Message = "Payment payload cannot be null.";
                    response.StatusCode = 400;
                    return response;
                }

                var studentProfile = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == paymentDto.StudentProfileId);

                if (studentProfile == null)
                {
                    response.Success = false;
                    response.Message = "Student profile not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var termExists = await _context.SchoolTerms
                    .AnyAsync(t => t.Id == paymentDto.TermId
                                && t.AcademicSessionId == paymentDto.AcademicSessionId);

                if (!termExists)
                {
                    response.Success = false;
                    response.Message = "The selected term does not belong to the specified academic session.";
                    response.StatusCode = 400;
                    return response;
                }

                // 👇 StudentPayments = school fees ONLY.
                // If there are no fees for this class/term, this isn't a fee payment.
                var totalBilledForTerm = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .Where(cfp => cfp.SchoolClassId == studentProfile.SchoolClassId &&
                                  cfp.Fee.TermId == paymentDto.TermId &&
                                  cfp.Fee.AcademicSessionId == paymentDto.AcademicSessionId)
                    .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                if (totalBilledForTerm <= 0)
                {
                    response.Success = false;
                    response.Message =
                        "This student has no school fees configured for this term. " +
                        "If this is for PTA, uniform, exam fee, or other non-tuition items, " +
                        "use the Other Income endpoint.";
                    response.StatusCode = 400;
                    return response;
                }

                var totalMinimumRequired = await _context.ClassFeePrices
                    .Where(cfp => cfp.SchoolClassId == studentProfile.SchoolClassId
                               && cfp.Fee.TermId == paymentDto.TermId)
                    .SumAsync(cfp => (decimal?)cfp.MinimumAmountToPay) ?? 0m;

                if (totalMinimumRequired > 0 && paymentDto.AmountPaid < totalMinimumRequired)
                {
                    response.Success = false;
                    response.Message =
                        $"Payment amount (₦{paymentDto.AmountPaid:N2}) is less than the " +
                        $"minimum required (₦{totalMinimumRequired:N2}).";
                    response.StatusCode = 400;
                    return response;
                }

                var reference =
                    $"DIBA-{DateTime.UtcNow:yyyyMMdd}-" +
                    $"{Guid.NewGuid().ToString()[..8].ToUpper()}";

                var payment = new StudentPayment
                {
                    Id = Guid.NewGuid(),
                    StudentProfileId = paymentDto.StudentProfileId,
                    AmountPaid = paymentDto.AmountPaid,
                    ReferenceNumber = reference,
                    Gateway = paymentDto.Gateway,
                    Status = PaymentStatus.Pending,
                    AcademicSessionId = paymentDto.AcademicSessionId,
                    TermId = paymentDto.TermId,
                    PaymentDate = DateTime.UtcNow,
                    ProofOfPaymentUrl = paymentDto.ProofOfPaymentUrl?.Trim(),
                };

                _context.StudentPayments.Add(payment);
                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    CurrentUserId,
                    "Payment Initialized",
                    $"Ref {reference}: ₦{paymentDto.AmountPaid:N2} by student " +
                    $"{paymentDto.StudentProfileId} via {paymentDto.Gateway}.",
                    Ip);

                if (!string.IsNullOrEmpty(studentProfile.UserId))
                {
                    await _notification.SendNotificationAsync(
                        studentProfile.UserId,
                        "Payment Initialized",
                        $"Your payment of ₦{paymentDto.AmountPaid:N2} " +
                        $"(Ref {reference}) is pending confirmation.",
                        category: "Payment");
                }

                response.Success = true;
                response.Message = "Payment initialized successfully.";
                response.StatusCode = 200;
                response.Data = reference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording student payment.");

                await _auditLog.LogActivityAsync(
                    CurrentUserId, "Payment Init FAILED", ex.Message, Ip);

                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // GET BY STUDENT — unchanged
        // ==========================================================
        #region GetPaymentsByStudentAsync
        public async Task<ServiceResponse<IEnumerable<StudentPaymentDto>>> GetPaymentsByStudentAsync(
            Guid studentProfileId)
        {
            var response = new ServiceResponse<IEnumerable<StudentPaymentDto>>();
            try
            {
                var payments = await _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .Where(p => p.StudentProfileId == studentProfileId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new StudentPaymentDto
                    {
                        Id = p.Id,
                        StudentProfileId = p.StudentProfileId,
                        StudentName = p.StudentProfile != null
                            ? $"{p.StudentProfile.FirstName} {p.StudentProfile.LastName}"
                            : "N/A",
                        AmountPaid = p.AmountPaid,
                        ReferenceNumber = p.ReferenceNumber,
                        Gateway = p.Gateway,
                        Status = p.Status,
                        SessionName = p.AcademicSession != null ? p.AcademicSession.Name : "N/A",
                        TermName = p.Term != null ? p.Term.Name : "N/A",
                        PaymentDate = p.PaymentDate,
                        ProofOfPaymentUrl = p.ProofOfPaymentUrl,
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = payments;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching payments for student profile: {StudentProfileId}",
                    studentProfileId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // GET BY ID — unchanged
        // ==========================================================
        #region GetPaymentByIdAsync
        public async Task<ServiceResponse<StudentPaymentDto>> GetPaymentByIdAsync(Guid paymentId)
        {
            var response = new ServiceResponse<StudentPaymentDto>();
            try
            {
                var p = await _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .FirstOrDefaultAsync(x => x.Id == paymentId);

                if (p == null)
                {
                    response.Success = false;
                    response.Message = "Payment record not found.";
                    response.StatusCode = 404;
                    return response;
                }

                response.Data = new StudentPaymentDto
                {
                    Id = p.Id,
                    StudentProfileId = p.StudentProfileId,
                    StudentName = p.StudentProfile != null
                        ? $"{p.StudentProfile.FirstName} {p.StudentProfile.LastName}"
                        : "N/A",
                    AmountPaid = p.AmountPaid,
                    ReferenceNumber = p.ReferenceNumber,
                    Gateway = p.Gateway,
                    Status = p.Status,
                    SessionName = p.AcademicSession?.Name ?? "N/A",
                    TermName = p.Term?.Name ?? "N/A",
                    PaymentDate = p.PaymentDate,
                    ProofOfPaymentUrl = p.ProofOfPaymentUrl,
                };
                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment by ID: {PaymentId}", paymentId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // GET BY REFERENCE — unchanged
        // ==========================================================
        #region GetPaymentByReferenceAsync
        public async Task<ServiceResponse<StudentPaymentDto>> GetPaymentByReferenceAsync(
            string referenceNumber)
        {
            var response = new ServiceResponse<StudentPaymentDto>();
            try
            {
                var p = await _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .FirstOrDefaultAsync(x => x.ReferenceNumber == referenceNumber);

                if (p == null)
                {
                    response.Success = false;
                    response.Message = "Payment record with this reference not found.";
                    response.StatusCode = 404;
                    return response;
                }

                response.Data = new StudentPaymentDto
                {
                    Id = p.Id,
                    StudentProfileId = p.StudentProfileId,
                    StudentName = p.StudentProfile != null
                        ? $"{p.StudentProfile.FirstName} {p.StudentProfile.LastName}"
                        : "N/A",
                    AmountPaid = p.AmountPaid,
                    ReferenceNumber = p.ReferenceNumber,
                    Gateway = p.Gateway,
                    Status = p.Status,
                    SessionName = p.AcademicSession?.Name ?? "N/A",
                    TermName = p.Term?.Name ?? "N/A",
                    PaymentDate = p.PaymentDate,
                    ProofOfPaymentUrl = p.ProofOfPaymentUrl,
                };
                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment by reference: {Ref}", referenceNumber);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // UPDATE STATUS — FIX 2: only allocate to fees if fees exist
        // ==========================================================
        #region UpdatePaymentStatusAsync
        public async Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(
            Guid paymentId, PaymentStatus status)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                var payment = await _context.StudentPayments
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    response.Success = false;
                    response.Message = "Payment record not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var oldStatus = payment.Status;
                payment.Status = status;
                _context.StudentPayments.Update(payment);

                // ---------- Transition INTO Successful ----------
                if (oldStatus != PaymentStatus.Successful && status == PaymentStatus.Successful)
                {
                    // 👇 FIX 2 — only touch enrollment when the term actually has fees.
                    // Prevents accidental enrollment changes for non-fee payments.
                    var studentClassId = await _context.Students
                        .Where(s => s.Id == payment.StudentProfileId)
                        .Select(s => s.SchoolClassId)
                        .FirstOrDefaultAsync();

                    var hasFees = await _context.ClassFeePrices
                        .Include(cfp => cfp.Fee)
                        .AnyAsync(cfp => cfp.SchoolClassId == studentClassId &&
                                         cfp.Fee.TermId == payment.TermId);

                    if (hasFees)
                    {
                        await AllocatePaymentToFeesAsync(payment);

                        var enrollment = await EnsureEnrollmentExistsAsync(
                            payment.StudentProfileId,
                            payment.AcademicSessionId,
                            payment.TermId);

                        if (enrollment != null)
                        {
                            await RecomputeEnrollmentTotalsAsync(
                                enrollment,
                                excludePaymentId: payment.Id,
                                addPaymentAmount: payment.AmountPaid);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Payment {Ref} marked Successful but no ClassFeePrices exist for " +
                            "term {TermId}. Skipping enrollment allocation.",
                            payment.ReferenceNumber, payment.TermId);
                    }
                }

                // ---------- Transition OUT of Successful ----------
                if (oldStatus == PaymentStatus.Successful && status != PaymentStatus.Successful)
                {
                    var allocations = await _context.StudentPaymentItems
                        .Where(pi => pi.StudentPaymentId == payment.Id)
                        .ToListAsync();
                    if (allocations.Any())
                        _context.StudentPaymentItems.RemoveRange(allocations);

                    var enrollment = await _context.StudentEnrollments
                        .FirstOrDefaultAsync(e =>
                            e.StudentProfileId == payment.StudentProfileId &&
                            e.TermId == payment.TermId &&
                            e.AcademicSessionId == payment.AcademicSessionId);

                    if (enrollment != null)
                    {
                        await RecomputeEnrollmentTotalsAsync(
                            enrollment,
                            excludePaymentId: payment.Id,
                            addPaymentAmount: 0m);
                    }
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    CurrentUserId,
                    "Payment Status Updated",
                    $"Ref {payment.ReferenceNumber}: {oldStatus} → {status}.",
                    Ip);

                var studentUserId = await _context.Students
                    .Where(s => s.Id == payment.StudentProfileId)
                    .Select(s => s.UserId)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(studentUserId))
                {
                    await _notification.SendNotificationAsync(
                        studentUserId,
                        $"Payment {status}",
                        $"Your payment (Ref {payment.ReferenceNumber}) status is now: {status}.",
                        category: "Payment");
                }

                response.Success = true;
                response.Message = "Payment status updated successfully.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for ID: {PaymentId}", paymentId);

                await _auditLog.LogActivityAsync(
                    CurrentUserId, "Payment Update FAILED", ex.Message, Ip);

                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // FLUTTERWAVE — INITIATE — unchanged
        // (fee-only by design — the student portal only uses this for fees)
        // ==========================================================
        #region InitiateFlutterwaveAsync
        public async Task<ServiceResponse<InitiateFlutterwaveResponseDto>> InitiateFlutterwaveAsync(
            InitiateFlutterwaveDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<InitiateFlutterwaveResponseDto>();

            try
            {
                var secretKey = FlutterwaveSecretKey;
                if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Contains("PASTE"))
                {
                    response.Success = false;
                    response.Message =
                        "Flutterwave is not configured. " +
                        "Add Flutterwave:SecretKey to appsettings.json.";
                    response.StatusCode = 500;
                    return response;
                }

                if (request.Amount <= 0)
                {
                    response.Success = false;
                    response.Message = "Amount must be greater than zero.";
                    response.StatusCode = 400;
                    return response;
                }

                var student = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.StudentProfileId);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var term = await _context.SchoolTerms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.Id == request.TermId &&
                        t.AcademicSessionId == request.AcademicSessionId);

                if (term == null)
                {
                    response.Success = false;
                    response.Message = "Invalid session or term.";
                    response.StatusCode = 400;
                    return response;
                }

                var email = await ResolveCustomerEmailAsync(
                    student.Id, student.UserId, request.CustomerEmail);

                if (string.IsNullOrWhiteSpace(email))
                {
                    response.Success = false;
                    response.Message =
                        "An email address is required for online payment. " +
                        "Please provide one or ask the school to add it to your profile.";
                    response.StatusCode = 400;
                    return response;
                }

                var reference =
                    $"DIBA-FLW-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                    $"{Guid.NewGuid().ToString()[..6].ToUpper()}";

                var payment = new StudentPayment
                {
                    Id = Guid.NewGuid(),
                    StudentProfileId = student.Id,
                    AmountPaid = request.Amount,
                    ReferenceNumber = reference,
                    Gateway = PaymentGateway.Flutterwave,
                    Status = PaymentStatus.Pending,
                    AcademicSessionId = request.AcademicSessionId,
                    TermId = request.TermId,
                    PaymentDate = DateTime.UtcNow,
                };

                _context.StudentPayments.Add(payment);
                await _context.SaveChangesAsync();

                var payload = new
                {
                    tx_ref = reference,
                    amount = request.Amount,
                    currency = FlutterwaveCurrency,
                    redirect_url = FlutterwaveRedirectUrl,
                    customer = new
                    {
                        email,
                        phonenumber = request.CustomerPhone ?? "",
                        name = $"{student.FirstName} {student.LastName}".Trim(),
                    },
                    customizations = new
                    {
                        title = "DIBA School Portal",
                        description =
                            $"Fee payment for {student.FirstName} {student.LastName} " +
                            $"({student.AdmissionNumber}) — {term.Name}",
                    },
                    meta = new
                    {
                        studentProfileId = student.Id,
                        academicSessionId = request.AcademicSessionId,
                        termId = request.TermId,
                        paymentId = payment.Id,
                    },
                };

                using var flwContent = JsonContent.Create(payload);
                using var flwResponse = await SendFlutterwaveRequestAsync(
                    HttpMethod.Post, "/payments", flwContent, secretKey);

                var bodyText = await flwResponse.Content.ReadAsStringAsync();

                if (!flwResponse.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Flutterwave initiate failed: {Status} {Body}",
                        (int)flwResponse.StatusCode, bodyText);

                    _context.StudentPayments.Remove(payment);
                    await _context.SaveChangesAsync();

                    response.Success = false;
                    response.Message =
                        "Flutterwave rejected the request. Please try again.";
                    response.StatusCode = 502;
                    return response;
                }

                string? checkoutUrl;
                using (var doc = JsonDocument.Parse(bodyText))
                {
                    checkoutUrl = doc.RootElement
                        .GetProperty("data")
                        .GetProperty("link")
                        .GetString();
                }

                if (string.IsNullOrWhiteSpace(checkoutUrl))
                {
                    _context.StudentPayments.Remove(payment);
                    await _context.SaveChangesAsync();

                    response.Success = false;
                    response.Message = "Flutterwave did not return a checkout URL.";
                    response.StatusCode = 502;
                    return response;
                }

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Flutterwave Payment Initiated",
                    $"Ref {reference}: ₦{request.Amount:N2} for " +
                    $"{student.FirstName} {student.LastName} ({student.AdmissionNumber}).",
                    ipAddress);

                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Redirect to checkout.";
                response.Data = new InitiateFlutterwaveResponseDto
                {
                    PaymentId = payment.Id,
                    ReferenceNumber = reference,
                    CheckoutUrl = checkoutUrl,
                    Amount = request.Amount,
                    Currency = FlutterwaveCurrency,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Flutterwave payment.");

                await _auditLog.LogActivityAsync(
                    currentUserId, "Flutterwave Initiate FAILED", ex.Message, ipAddress);

                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // FLUTTERWAVE — VERIFY — FIX 3: only allocate if fees exist
        // ==========================================================
        #region VerifyAndCompleteFlutterwavePaymentAsync
        public async Task<ServiceResponse<bool>> VerifyAndCompleteFlutterwavePaymentAsync(
            string transactionId, string referenceNumber)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                var paymentRecord = await _context.StudentPayments
                    .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNumber);

                if (paymentRecord == null)
                {
                    response.Success = false;
                    response.Message =
                        "Local payment record matching reference was not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (paymentRecord.Status == PaymentStatus.Successful)
                {
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Message = "Already verified.";
                    response.Data = true;
                    return response;
                }

                var secretKey = FlutterwaveSecretKey;
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    response.Success = false;
                    response.Message = "Flutterwave is not configured.";
                    response.StatusCode = 500;
                    return response;
                }

                using var fwResponse = await SendFlutterwaveRequestAsync(
                    HttpMethod.Get,
                    $"/transactions/{transactionId}/verify",
                    content: null,
                    secretKey);

                if (!fwResponse.IsSuccessStatusCode)
                {
                    response.Success = false;
                    response.Message =
                        "Failed to communicate with Flutterwave verification server.";
                    response.StatusCode = 400;
                    return response;
                }

                var jsonResult =
                    await fwResponse.Content.ReadFromJsonAsync<FlutterwaveApiResponseWrapper>();

                if (jsonResult?.Data == null ||
                    jsonResult.Data.Status.ToLower() != "successful")
                {
                    paymentRecord.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();

                    response.Success = false;
                    response.Message = "Transaction was not successful on Flutterwave.";
                    response.StatusCode = 400;
                    return response;
                }

                if (jsonResult.Data.Currency != FlutterwaveCurrency)
                {
                    response.Success = false;
                    response.Message = "Invalid transaction currency.";
                    response.StatusCode = 400;
                    return response;
                }

                if (jsonResult.Data.Amount < paymentRecord.AmountPaid)
                {
                    response.Success = false;
                    response.Message =
                        "Paid amount is less than the required school fee amount.";
                    response.StatusCode = 400;
                    return response;
                }

                // 👇 FIX 3 — verify the term actually has fees before enrolling.
                // A Flutterwave payment without a fee schedule is a data problem;
                // don't mark it successful because there's nothing to allocate it to.
                var studentClassId = await _context.Students
                    .Where(s => s.Id == paymentRecord.StudentProfileId)
                    .Select(s => s.SchoolClassId)
                    .FirstOrDefaultAsync();

                var hasFees = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .AnyAsync(cfp => cfp.SchoolClassId == studentClassId &&
                                     cfp.Fee.TermId == paymentRecord.TermId);

                if (!hasFees)
                {
                    _logger.LogError(
                        "Flutterwave payment {Ref} verified but no ClassFeePrices exist for " +
                        "student {StudentId} term {TermId}.",
                        referenceNumber, paymentRecord.StudentProfileId, paymentRecord.TermId);

                    paymentRecord.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();

                    response.Success = false;
                    response.Message =
                        "Payment received but the student has no fees configured for this term. " +
                        "Please contact the school to resolve.";
                    response.StatusCode = 400;
                    return response;
                }

                paymentRecord.Status = PaymentStatus.Successful;
                _context.StudentPayments.Update(paymentRecord);

                await AllocatePaymentToFeesAsync(paymentRecord);

                var enrollment = await EnsureEnrollmentExistsAsync(
                    paymentRecord.StudentProfileId,
                    paymentRecord.AcademicSessionId,
                    paymentRecord.TermId);

                if (enrollment == null)
                {
                    response.Success = false;
                    response.Message = "Student not found — cannot create enrollment.";
                    response.StatusCode = 404;
                    return response;
                }

                await RecomputeEnrollmentTotalsAsync(
                    enrollment,
                    excludePaymentId: paymentRecord.Id,
                    addPaymentAmount: paymentRecord.AmountPaid);

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    "System/Flutterwave",
                    "Payment Verified",
                    $"Ref {referenceNumber}: ₦{paymentRecord.AmountPaid:N2} verified " +
                    $"(TxId {transactionId}).",
                    Ip);

                var studentUserId = await _context.Students
                    .Where(s => s.Id == paymentRecord.StudentProfileId)
                    .Select(s => s.UserId)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(studentUserId))
                {
                    await _notification.SendNotificationAsync(
                        studentUserId,
                        "Payment Successful",
                        $"Your payment of ₦{paymentRecord.AmountPaid:N2} " +
                        $"(Ref {referenceNumber}) was confirmed.",
                        category: "Payment");
                }

                response.Success = true;
                response.Message =
                    "Flutterwave payment verified and marked as successful.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error verifying Flutterwave transaction {TxId}", transactionId);

                await _auditLog.LogActivityAsync(
                    "System/Flutterwave", "Payment Verify FAILED", ex.Message, Ip);

                response.Success = false;
                response.Message = $"An error occurred during verification: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // PRIVATE HELPERS — all unchanged
        // ==========================================================

        #region CalculatePreviousOutstandingDebtAsync
        private async Task<decimal> CalculatePreviousOutstandingDebtAsync(
            Guid studentProfileId, DateTime? currentTermStartDate)
        {
            var pastEnrollments = await _context.StudentEnrollments
                .Include(e => e.Term)
                .Where(e => e.StudentProfileId == studentProfileId
                         && e.Term.StartDate < currentTermStartDate)
                .AsNoTracking()
                .ToListAsync();

            decimal totalPastBilled = 0;
            decimal totalPastPaid = 0;

            foreach (var past in pastEnrollments)
            {
                var pastFees = await _context.ClassFeePrices
                    .Where(cfp => cfp.SchoolClassId == past.SchoolClassId
                               && cfp.Fee.TermId == past.TermId)
                    .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0;

                totalPastBilled += pastFees;

                var pastPayments = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId
                             && p.TermId == past.TermId
                             && p.Status == PaymentStatus.Successful)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

                totalPastPaid += pastPayments;
            }

            return Math.Max(0, totalPastBilled - totalPastPaid);
        }
        #endregion

        #region AllocatePaymentToFeesAsync
        private async Task AllocatePaymentToFeesAsync(StudentPayment payment)
        {
            var existing = await _context.StudentPaymentItems
                .Where(pi => pi.StudentPaymentId == payment.Id)
                .ToListAsync();
            if (existing.Any())
                _context.StudentPaymentItems.RemoveRange(existing);

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == payment.StudentProfileId);
            if (student == null) return;

            var fees = await _context.ClassFeePrices
                .Include(cfp => cfp.Fee)
                .Where(cfp => cfp.SchoolClassId == student.SchoolClassId
                           && cfp.Fee.TermId == payment.TermId)
                .OrderBy(cfp => cfp.Fee.FeeName)
                .ToListAsync();

            if (!fees.Any()) return;

            var priorByFee = await _context.StudentPaymentItems
                .Where(pi =>
                    pi.StudentPayment != null &&
                    pi.StudentPayment.StudentProfileId == payment.StudentProfileId &&
                    pi.StudentPayment.TermId == payment.TermId &&
                    pi.StudentPayment.Status == PaymentStatus.Successful &&
                    pi.StudentPaymentId != payment.Id &&
                    pi.FeeId != null)
                .GroupBy(pi => pi.FeeId!.Value)
                .Select(g => new { FeeId = g.Key, Total = g.Sum(pi => pi.AmountAllocated) })
                .ToDictionaryAsync(x => x.FeeId, x => x.Total);

            var remaining = payment.AmountPaid;
            var allocations = new Dictionary<Guid, decimal>();

            foreach (var cfp in fees)
            {
                if (remaining <= 0) break;
                priorByFee.TryGetValue(cfp.FeeId, out var alreadyPaid);
                var toMin = Math.Max(0, cfp.MinimumAmountToPay - alreadyPaid);
                var take = Math.Min(remaining, toMin);
                if (take > 0)
                {
                    allocations[cfp.FeeId] = take;
                    remaining -= take;
                }
            }

            foreach (var cfp in fees)
            {
                if (remaining <= 0) break;
                priorByFee.TryGetValue(cfp.FeeId, out var alreadyPaid);
                allocations.TryGetValue(cfp.FeeId, out var paidSoFar);

                var toFull = Math.Max(0, cfp.Amount - alreadyPaid - paidSoFar);
                var take = Math.Min(remaining, toFull);
                if (take > 0)
                {
                    allocations[cfp.FeeId] = paidSoFar + take;
                    remaining -= take;
                }
            }

            if (remaining > 0)
            {
                var firstId = fees.First().FeeId;
                allocations.TryGetValue(firstId, out var current);
                allocations[firstId] = current + remaining;
            }

            foreach (var (feeId, amount) in allocations)
            {
                _context.StudentPaymentItems.Add(new StudentPaymentItem
                {
                    Id = Guid.NewGuid(),
                    StudentPaymentId = payment.Id,
                    FeeId = feeId,
                    AmountAllocated = amount,
                });
            }
        }
        #endregion

        #region EnsureEnrollmentExistsAsync
        private async Task<StudentEnrollment?> EnsureEnrollmentExistsAsync(
            Guid studentProfileId, Guid academicSessionId, Guid termId)
        {
            var enrollment = await _context.StudentEnrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentProfileId == studentProfileId &&
                    e.TermId == termId &&
                    e.AcademicSessionId == academicSessionId);

            if (enrollment != null) return enrollment;

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == studentProfileId);

            if (student == null)
            {
                _logger.LogWarning(
                    "Cannot create enrollment — student {StudentId} not found.",
                    studentProfileId);
                return null;
            }

            var billedAmount = await _context.ClassFeePrices
                .Include(cfp => cfp.Fee)
                .Where(cfp => cfp.SchoolClassId == student.SchoolClassId &&
                              cfp.Fee.TermId == termId &&
                              cfp.Fee.AcademicSessionId == academicSessionId)
                .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

            enrollment = new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                StudentProfileId = student.Id,
                AcademicSessionId = academicSessionId,
                TermId = termId,
                SchoolClassId = student.SchoolClassId,
                TotalAmountPaid = 0m,
                OutstandingBalance = billedAmount,
                IsFullyPaid = false,
                Status = StudentEnrollmentStatus.Pending,
                IsActive = false,
                EnrolledAt = DateTime.UtcNow,
            };

            _context.StudentEnrollments.Add(enrollment);

            _logger.LogInformation(
                "Auto-created enrollment for student {StudentId} term {TermId} — " +
                "billed ₦{Billed:N2}.",
                student.Id, termId, billedAmount);

            return enrollment;
        }
        #endregion

        #region RecomputeEnrollmentTotalsAsync
        private async Task RecomputeEnrollmentTotalsAsync(
            StudentEnrollment enrollment,
            Guid excludePaymentId,
            decimal addPaymentAmount)
        {
            var pastPaid = await _context.StudentPayments
                .Where(p => p.StudentProfileId == enrollment.StudentProfileId &&
                            p.TermId == enrollment.TermId &&
                            p.Status == PaymentStatus.Successful &&
                            p.Id != excludePaymentId)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

            var newTotalPaid = pastPaid + addPaymentAmount;

            enrollment.TotalAmountPaid = newTotalPaid;

            enrollment.OutstandingBalance =
                Math.Max(0, enrollment.OutstandingBalance - addPaymentAmount);

            enrollment.IsFullyPaid = enrollment.OutstandingBalance <= 0;

            var totalMinimum = await _context.ClassFeePrices
                .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId &&
                              cfp.Fee.TermId == enrollment.TermId)
                .SumAsync(cfp => (decimal?)cfp.MinimumAmountToPay) ?? 0m;

            if (totalMinimum > 0)
            {
                if (newTotalPaid < totalMinimum)
                {
                    enrollment.Status = StudentEnrollmentStatus.Pending;
                    enrollment.IsActive = false;
                }
                else
                {
                    enrollment.Status = StudentEnrollmentStatus.Active;
                    enrollment.IsActive = true;
                }
            }

            _context.StudentEnrollments.Update(enrollment);
        }
        #endregion

        #region ResolveCustomerEmailAsync
        private async Task<string?> ResolveCustomerEmailAsync(
            Guid studentProfileId, string studentUserId, string? providedEmail)
        {
            if (!string.IsNullOrWhiteSpace(providedEmail))
                return providedEmail.Trim();

            var studentEmail = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == studentUserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(studentEmail))
                return studentEmail;

            return await _context.ParentStudents
                .AsNoTracking()
                .Include(ps => ps.Parent)
                .Where(ps => ps.StudentProfileId == studentProfileId
                          && ps.IsPrimaryContact)
                .Select(ps => ps.Parent!.Email)
                .FirstOrDefaultAsync();
        }
        #endregion

        #region SendFlutterwaveRequestAsync
        private async Task<HttpResponseMessage> SendFlutterwaveRequestAsync(
            HttpMethod method,
            string path,
            HttpContent? content,
            string secretKey)
        {
            using var request = new HttpRequestMessage(
                method, $"{FlutterwaveBaseUrl}{path}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", secretKey);

            if (content != null)
                request.Content = content;

            return await _httpClient.SendAsync(request);
        }
        #endregion
    }
}