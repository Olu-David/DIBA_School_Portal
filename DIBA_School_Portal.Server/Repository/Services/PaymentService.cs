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
using System.Net.Http.Json;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IAuditLog _auditLog;
        private readonly INotificationLog _notification;
        private readonly IHttpContextAccessor _http;

        #region Constructor
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
        #endregion

        // ==========================================================
        // BILL BREAKDOWN — works with or without enrollment
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

                var schoolClass = student.SchoolClass;

                decimal previousDebt = await CalculatePreviousOutstandingDebtAsync(
                    studentProfileId, term.StartDate);

                var classFeePrices = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .Where(cfp => cfp.SchoolClassId == student.SchoolClassId
                               && cfp.Fee.TermId == termId)
                    .AsNoTracking()
                    .ToListAsync();

                var classBooks = await _context.ClassBooks
                    .Where(cb => cb.SchoolClassId == student.SchoolClassId
                              && cb.TermId == termId)
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
                    decimal itemAmount = cfp.Amount;
                    currentTermBilled += itemAmount;

                    decimal paidTowardThis = successfulPayments
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
                        IsFullyPaid = paidTowardThis >= itemAmount
                    });
                }

                var bookBreakdowns = new List<BookItemBreakdownDto>();
                foreach (var book in classBooks)
                {
                    currentTermBilled += book.Price;

                    bool isBookPaid = successfulPayments
                        .SelectMany(p => p.PaymentItems)
                        .Any(pi => pi.ClassBookId == book.Id);

                    bookBreakdowns.Add(new BookItemBreakdownDto
                    {
                        ClassBookId = book.Id,
                        Title = book.Title,
                        Description = book.Description,
                        Price = book.Price,
                        IsPurchased = isBookPaid
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
                    PaymentDate = p.PaymentDate
                }).ToList();

                response.Data = new StudentBillResponseDto
                {
                    StudentProfileId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    ClassName = schoolClass?.Name ?? "—",
                    TermId = termId,
                    AcademicSessionId = term.AcademicSessionId,
                    TotalBilledAmount = totalBilled,
                    TotalPaidSoFar = totalPaidSoFar,
                    TotalBalanceRemaining = balanceRemaining,
                    MinimumAmountRequired = totalMinimumRequired,
                    FeeItems = feeBreakdowns,
                    AvailableBooks = bookBreakdowns,
                    PaymentHistory = paymentHistory
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

        #region Helper: CalculatePreviousOutstandingDebtAsync
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

            foreach (var pastEnrollment in pastEnrollments)
            {
                var pastFees = await _context.ClassFeePrices
                    .Where(cfp => cfp.SchoolClassId == pastEnrollment.SchoolClassId
                               && cfp.Fee.TermId == pastEnrollment.TermId)
                    .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0;

                var pastBooks = await _context.ClassBooks
                    .Where(cb => cb.SchoolClassId == pastEnrollment.SchoolClassId
                              && cb.TermId == pastEnrollment.TermId)
                    .SumAsync(cb => (decimal?)cb.Price) ?? 0;

                totalPastBilled += (pastFees + pastBooks);

                var pastPayments = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId
                             && p.TermId == pastEnrollment.TermId
                             && p.Status == PaymentStatus.Successful)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

                totalPastPaid += pastPayments;
            }

            return Math.Max(0, totalPastBilled - totalPastPaid);
        }
        #endregion

        // ==========================================================
        // INITIALIZE PAYMENT
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

                var classFeePrice = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .FirstOrDefaultAsync(cfp => cfp.SchoolClassId == studentProfile.SchoolClassId
                                             && cfp.Fee.TermId == paymentDto.TermId
                                             && (cfp.Fee.FeeName.ToLower().Contains("tuition")
                                              || cfp.Fee.FeeName.ToLower().Contains("school fee")));

                if (classFeePrice == null)
                {
                    classFeePrice = await _context.ClassFeePrices
                        .Include(cfp => cfp.Fee)
                        .FirstOrDefaultAsync(cfp => cfp.SchoolClassId == studentProfile.SchoolClassId
                                                 && cfp.Fee.TermId == paymentDto.TermId);
                }

                if (classFeePrice != null && paymentDto.AmountPaid < classFeePrice.MinimumAmountToPay)
                {
                    response.Success = false;
                    response.Message = $"Payment amount ({paymentDto.AmountPaid:N2}) is less than the minimum required amount ({classFeePrice.MinimumAmountToPay:N2}).";
                    response.StatusCode = 400;
                    return response;
                }

                string secureReference = $"DIBA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                var payment = new StudentPayment
                {
                    Id = Guid.NewGuid(),
                    StudentProfileId = paymentDto.StudentProfileId,
                    AmountPaid = paymentDto.AmountPaid,
                    ReferenceNumber = secureReference,
                    Gateway = paymentDto.Gateway,
                    Status = PaymentStatus.Pending,
                    AcademicSessionId = paymentDto.AcademicSessionId,
                    TermId = paymentDto.TermId,
                    PaymentDate = DateTime.UtcNow,
                    ProofOfPaymentUrl = paymentDto.ProofOfPaymentUrl?.Trim()
                };

                await _context.StudentPayments.AddAsync(payment);
                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System",
                    "Payment Initialized",
                    $"Ref {secureReference}: ₦{paymentDto.AmountPaid:N2} by student {paymentDto.StudentProfileId} via {paymentDto.Gateway}.",
                    Ip);

                if (!string.IsNullOrEmpty(studentProfile.UserId))
                {
                    await _notification.SendNotificationAsync(
                        studentProfile.UserId,
                        "Payment Initialized",
                        $"Your payment of ₦{paymentDto.AmountPaid:N2} (Ref {secureReference}) is pending confirmation.",
                        category: "Payment");
                }

                response.Success = true;
                response.Message = "Payment initialized successfully.";
                response.StatusCode = 200;
                response.Data = secureReference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording student payment.");

                await _auditLog.LogActivityAsync(
                    _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System",
                    "Payment Init FAILED",
                    ex.Message,
                    Ip);

                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // GET BY STUDENT
        // ==========================================================
        #region GetPaymentsByStudentAsync
        public async Task<ServiceResponse<IEnumerable<StudentPaymentDto>>> GetPaymentsByStudentAsync(Guid studentProfileId)
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
                        ProofOfPaymentUrl = p.ProofOfPaymentUrl
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = payments;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payments for student profile: {StudentProfileId}", studentProfileId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // GET BY ID
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
                    SessionName = p.AcademicSession != null ? p.AcademicSession.Name : "N/A",
                    TermName = p.Term != null ? p.Term.Name : "N/A",
                    PaymentDate = p.PaymentDate,
                    ProofOfPaymentUrl = p.ProofOfPaymentUrl
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
        // UPDATE STATUS — auto-creates / updates enrollment
        // ==========================================================
        #region UpdatePaymentStatusAsync
        public async Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status)
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

                // ---------- Transition to Successful → apply & recompute ----------
                if (oldStatus != PaymentStatus.Successful && status == PaymentStatus.Successful)
                {
                    var enrollment = await _context.StudentEnrollments
                        .FirstOrDefaultAsync(e => e.StudentProfileId == payment.StudentProfileId
                                               && e.TermId == payment.TermId
                                               && e.AcademicSessionId == payment.AcademicSessionId);

                    if (enrollment == null)
                    {
                        var student = await _context.Students
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == payment.StudentProfileId);

                        if (student != null)
                        {
                            var billedAmount = await _context.ClassFeePrices
                                .Include(cfp => cfp.Fee)
                                .Where(cfp => cfp.SchoolClassId == student.SchoolClassId
                                           && cfp.Fee.TermId == payment.TermId
                                           && cfp.Fee.AcademicSessionId == payment.AcademicSessionId)
                                .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                            enrollment = new StudentEnrollments
                            {
                                Id = Guid.NewGuid(),
                                StudentProfileId = student.Id,
                                AcademicSessionId = payment.AcademicSessionId,
                                TermId = payment.TermId,
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
                                "Auto-created enrollment for student {StudentId} term {TermId} — billed ₦{Billed:N2}.",
                                student.Id, payment.TermId, billedAmount);
                        }
                    }

                    if (enrollment != null)
                    {
                        // Sum all other successful payments (exclude this one from DB to avoid double count)
                        var pastPaid = await _context.StudentPayments
                            .Where(p => p.StudentProfileId == payment.StudentProfileId
                                     && p.TermId == payment.TermId
                                     && p.Status == PaymentStatus.Successful
                                     && p.Id != payment.Id)
                            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

                        var newTotalPaid = pastPaid + payment.AmountPaid;

                        var totalRequiredFee = await _context.ClassFeePrices
                            .Include(cfp => cfp.Fee)
                            .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId
                                       && cfp.Fee.TermId == payment.TermId)
                            .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                        enrollment.TotalAmountPaid = newTotalPaid;
                        enrollment.OutstandingBalance = Math.Max(0, totalRequiredFee - newTotalPaid);

                        var tuitionFeePrice = await _context.ClassFeePrices
                            .Include(cfp => cfp.Fee)
                            .FirstOrDefaultAsync(cfp =>
                                cfp.SchoolClassId == enrollment.SchoolClassId &&
                                cfp.Fee.TermId == payment.TermId &&
                                (cfp.Fee.FeeName.ToLower().Contains("tuition") ||
                                 cfp.Fee.FeeName.ToLower().Contains("school fee")));

                        if (tuitionFeePrice == null)
                        {
                            tuitionFeePrice = await _context.ClassFeePrices
                                .Include(cfp => cfp.Fee)
                                .FirstOrDefaultAsync(cfp =>
                                    cfp.SchoolClassId == enrollment.SchoolClassId &&
                                    cfp.Fee.TermId == payment.TermId);
                        }

                        if (tuitionFeePrice != null)
                        {
                            if (newTotalPaid < tuitionFeePrice.MinimumAmountToPay)
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

                        enrollment.IsFullyPaid = totalRequiredFee > 0 && newTotalPaid >= totalRequiredFee;

                        _context.StudentEnrollments.Update(enrollment);
                    }
                }

                // ---------- Transition FROM Successful → reverse ----------
                if (oldStatus == PaymentStatus.Successful && status != PaymentStatus.Successful)
                {
                    var enrollment = await _context.StudentEnrollments
                        .FirstOrDefaultAsync(e => e.StudentProfileId == payment.StudentProfileId
                                               && e.TermId == payment.TermId
                                               && e.AcademicSessionId == payment.AcademicSessionId);

                    if (enrollment != null)
                    {
                        var remainingPaid = await _context.StudentPayments
                            .Where(p => p.StudentProfileId == payment.StudentProfileId
                                     && p.TermId == payment.TermId
                                     && p.Status == PaymentStatus.Successful
                                     && p.Id != payment.Id)
                            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

                        var totalRequiredFee = await _context.ClassFeePrices
                            .Include(cfp => cfp.Fee)
                            .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId
                                       && cfp.Fee.TermId == payment.TermId)
                            .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                        enrollment.TotalAmountPaid = remainingPaid;
                        enrollment.OutstandingBalance = Math.Max(0, totalRequiredFee - remainingPaid);
                        enrollment.IsFullyPaid = totalRequiredFee > 0 && remainingPaid >= totalRequiredFee;

                        _context.StudentEnrollments.Update(enrollment);
                    }
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System",
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
                    _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System",
                    "Payment Update FAILED",
                    ex.Message,
                    Ip);

                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        // ==========================================================
        // GET BY REFERENCE
        // ==========================================================
        #region GetPaymentByReferenceAsync
        public async Task<ServiceResponse<StudentPaymentDto>> GetPaymentByReferenceAsync(string referenceNumber)
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
                    SessionName = p.AcademicSession != null ? p.AcademicSession.Name : "N/A",
                    TermName = p.Term != null ? p.Term.Name : "N/A",
                    PaymentDate = p.PaymentDate,
                    ProofOfPaymentUrl = p.ProofOfPaymentUrl
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
        // FLUTTERWAVE VERIFY — auto-creates / updates enrollment
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
                    response.Message = "Local payment record matching reference was not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var secretKey = _configuration["Flutterwave:SecretKey"];
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

                string url = $"https://api.flutterwave.com/v3/transactions/{transactionId}/verify";
                var fwResponse = await _httpClient.GetAsync(url);

                if (!fwResponse.IsSuccessStatusCode)
                {
                    response.Success = false;
                    response.Message = "Failed to communicate with Flutterwave verification server.";
                    response.StatusCode = 400;
                    return response;
                }

                var jsonResult = await fwResponse.Content.ReadFromJsonAsync<FlutterwaveApiResponseWrapper>();

                if (jsonResult?.Data == null || jsonResult.Data.Status.ToLower() != "successful")
                {
                    paymentRecord.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();

                    response.Success = false;
                    response.Message = "Transaction was not successful on Flutterwave.";
                    response.StatusCode = 400;
                    return response;
                }

                if (jsonResult.Data.Currency != "NGN")
                {
                    response.Success = false;
                    response.Message = "Invalid transaction currency.";
                    response.StatusCode = 400;
                    return response;
                }

                if (jsonResult.Data.Amount < paymentRecord.AmountPaid)
                {
                    response.Success = false;
                    response.Message = "Paid amount is less than the required school fee amount.";
                    response.StatusCode = 400;
                    return response;
                }

                // Mark payment successful
                paymentRecord.Status = PaymentStatus.Successful;
                _context.StudentPayments.Update(paymentRecord);

                // ==========================================================
                // 1. LOOK UP ENROLLMENT
                // ==========================================================
                var enrollment = await _context.StudentEnrollments
                    .FirstOrDefaultAsync(e =>
                        e.StudentProfileId == paymentRecord.StudentProfileId &&
                        e.TermId == paymentRecord.TermId &&
                        e.AcademicSessionId == paymentRecord.AcademicSessionId);

                // ==========================================================
                // 2. IF NULL → CREATE IT (billed from ClassFeePrices)
                // ==========================================================
                if (enrollment == null)
                {
                    var student = await _context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == paymentRecord.StudentProfileId);

                    if (student == null)
                    {
                        _logger.LogWarning(
                            "Cannot create enrollment — student {StudentId} not found.",
                            paymentRecord.StudentProfileId);

                        response.Success = false;
                        response.Message = "Student not found — cannot create enrollment.";
                        response.StatusCode = 404;
                        return response;
                    }

                    var billedAmount = await _context.ClassFeePrices
                        .Include(cfp => cfp.Fee)
                        .Where(cfp => cfp.SchoolClassId == student.SchoolClassId &&
                                      cfp.Fee.TermId == paymentRecord.TermId &&
                                      cfp.Fee.AcademicSessionId == paymentRecord.AcademicSessionId)
                        .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                    enrollment = new StudentEnrollments
                    {
                        Id = Guid.NewGuid(),
                        StudentProfileId = student.Id,
                        AcademicSessionId = paymentRecord.AcademicSessionId,
                        TermId = paymentRecord.TermId,
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
                        "Auto-created enrollment for student {StudentId} term {TermId} — billed ₦{Billed:N2}.",
                        student.Id, paymentRecord.TermId, billedAmount);
                }

                // ==========================================================
                // 3. APPLY THIS PAYMENT + RECALCULATE (both paths)
                // ==========================================================
                var pastPaid = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == paymentRecord.StudentProfileId &&
                                p.TermId == paymentRecord.TermId &&
                                p.Status == PaymentStatus.Successful &&
                                p.Id != paymentRecord.Id)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

                var newTotalPaid = pastPaid + paymentRecord.AmountPaid;

                var totalRequiredFee = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId &&
                                  cfp.Fee.TermId == paymentRecord.TermId)
                    .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                enrollment.TotalAmountPaid = newTotalPaid;
                enrollment.OutstandingBalance = Math.Max(0, totalRequiredFee - newTotalPaid);

                // ==========================================================
                // 4. STATUS FLAGS
                // ==========================================================
                var tuitionFeePrice = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .FirstOrDefaultAsync(cfp =>
                        cfp.SchoolClassId == enrollment.SchoolClassId &&
                        cfp.Fee.TermId == paymentRecord.TermId &&
                        (cfp.Fee.FeeName.ToLower().Contains("tuition") ||
                         cfp.Fee.FeeName.ToLower().Contains("school fee")));

                if (tuitionFeePrice == null)
                {
                    tuitionFeePrice = await _context.ClassFeePrices
                        .Include(cfp => cfp.Fee)
                        .FirstOrDefaultAsync(cfp =>
                            cfp.SchoolClassId == enrollment.SchoolClassId &&
                            cfp.Fee.TermId == paymentRecord.TermId);
                }

                if (tuitionFeePrice != null)
                {
                    if (newTotalPaid < tuitionFeePrice.MinimumAmountToPay)
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

                enrollment.IsFullyPaid = totalRequiredFee > 0 && newTotalPaid >= totalRequiredFee;

                _context.StudentEnrollments.Update(enrollment);

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    "System/Flutterwave",
                    "Payment Verified",
                    $"Ref {referenceNumber}: ₦{paymentRecord.AmountPaid:N2} verified (TxId {transactionId}).",
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
                        $"Your payment of ₦{paymentRecord.AmountPaid:N2} (Ref {referenceNumber}) was confirmed.",
                        category: "Payment");
                }

                response.Success = true;
                response.Message = "Flutterwave payment verified and marked as successful.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Flutterwave transaction {TxId}", transactionId);

                await _auditLog.LogActivityAsync(
                    "System/Flutterwave",
                    "Payment Verify FAILED",
                    ex.Message,
                    Ip);

                response.Success = false;
                response.Message = $"An error occurred during verification: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion
    }
}