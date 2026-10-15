using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class AccountService : IAccountService
    {
        #region Fields
        private readonly AppDbContext _context;
        private readonly IAuditLog _auditLog;
        private readonly IPaymentLog _paymentLog;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AccountService> _logger;

        private static readonly PaymentGateway[] OnlineGateways =
        {
            PaymentGateway.Flutterwave,
            PaymentGateway.Paystack,
        };
        #endregion

        #region Constructor
        public AccountService(
            AppDbContext context,
            IAuditLog auditLog,
            IPaymentLog paymentLog,
            IHttpContextAccessor http,
            ILogger<AccountService> logger)
        {
            _context = context;
            _auditLog = auditLog;
            _paymentLog = paymentLog;
            _http = http;
            _logger = logger;
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        #endregion

        // ==========================================================
        // DASHBOARD
        // ==========================================================
        #region GetDashboardAsync
        public async Task<ServiceResponse<FinanceDashboardDto>> GetDashboardAsync(
            Guid academicSessionId, Guid termId)
        {
            var response = new ServiceResponse<FinanceDashboardDto>();

            try
            {
                var session = await _context.AcademicSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == academicSessionId);

                if (session == null)
                {
                    response.Success = false;
                    response.Message = "Academic session not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var term = await _context.SchoolTerms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == termId
                                           && t.AcademicSessionId == academicSessionId);

                if (term == null)
                {
                    response.Success = false;
                    response.Message = "Term not found or does not belong to session.";
                    response.StatusCode = 404;
                    return response;
                }

                var enrollments = await _context.StudentEnrollments
                    .AsNoTracking()
                    .Include(e => e.StudentProfile)
                        .ThenInclude(sp => sp.SchoolClass)
                    .Include(e => e.SchoolClass)
                    .Where(e => e.TermId == termId && e.AcademicSessionId == academicSessionId)
                    .ToListAsync();

                var payments = await _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                        .ThenInclude(sp => sp.SchoolClass)
                    .Include(p => p.Term)
                    .Where(p => p.TermId == termId && p.AcademicSessionId == academicSessionId)
                    .ToListAsync();

                var successfulPayments = payments
                    .Where(p => p.Status == PaymentStatus.Successful)
                    .ToList();

                decimal totalCollected = successfulPayments.Sum(p => p.AmountPaid);
                decimal totalExpected = enrollments.Sum(e => e.TotalAmountPaid + e.OutstandingBalance);
                decimal totalOutstanding = Math.Max(0, totalExpected - totalCollected);
                decimal collectionRate = totalExpected > 0
                    ? Math.Round(totalCollected / totalExpected * 100, 2)
                    : 0m;

                decimal totalOnline = successfulPayments
                    .Where(p => OnlineGateways.Contains(p.Gateway))
                    .Sum(p => p.AmountPaid);
                decimal totalCash = successfulPayments
                    .Where(p => p.Gateway == PaymentGateway.Cash)
                    .Sum(p => p.AmountPaid);
                decimal totalBankTransfer = successfulPayments
                    .Where(p => p.Gateway == PaymentGateway.BankTransfer)
                    .Sum(p => p.AmountPaid);
                decimal totalManual = successfulPayments
                    .Where(p => p.Gateway == PaymentGateway.Manual)
                    .Sum(p => p.AmountPaid);

                int totalStudents = enrollments.Count;
                int fullyPaid = enrollments.Count(e => e.IsFullyPaid);
                int unpaid = enrollments.Count(e => e.TotalAmountPaid == 0);
                int partiallyPaid = totalStudents - fullyPaid - unpaid;

                int totalTx = payments.Count;
                int successTx = payments.Count(p => p.Status == PaymentStatus.Successful);
                int pendingTx = payments.Count(p => p.Status == PaymentStatus.Pending);
                int failedTx = payments.Count(p => p.Status == PaymentStatus.Failed);

                var classBreakdown = enrollments
                    .GroupBy(e => e.SchoolClassId)
                    .Select(g =>
                    {
                        var first = g.First();
                        var exp = g.Sum(e => e.TotalAmountPaid + e.OutstandingBalance);
                        var col = g.Sum(e => e.TotalAmountPaid);
                        return new ClassFinanceSummaryDto
                        {
                            ClassId = g.Key,
                            ClassName = first.SchoolClass?.Name ?? "—",
                            StudentCount = g.Count(),
                            Expected = exp,
                            Collected = col,
                            Outstanding = Math.Max(0, exp - col),
                            CollectionRate = exp > 0 ? Math.Round(col / exp * 100, 2) : 0m,
                            FullyPaidCount = g.Count(e => e.IsFullyPaid),
                            UnpaidCount = g.Count(e => e.TotalAmountPaid == 0),
                        };
                    })
                    .OrderBy(c => c.ClassName)
                    .ToList();

                var gatewayBreakdown = successfulPayments
                    .GroupBy(p => p.Gateway)
                    .Select(g =>
                    {
                        var amt = g.Sum(p => p.AmountPaid);
                        return new GatewaySummaryDto
                        {
                            Gateway = g.Key,
                            TransactionCount = g.Count(),
                            TotalAmount = amt,
                            Percentage = totalCollected > 0
                                ? Math.Round(amt / totalCollected * 100, 2)
                                : 0m,
                        };
                    })
                    .OrderByDescending(g => g.TotalAmount)
                    .ToList();

                var recent = payments
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .Select(p => new FinancePaymentRowDto
                    {
                        Id = p.Id,
                        ReferenceNumber = p.ReferenceNumber,
                        StudentName = p.StudentProfile != null
                            ? $"{p.StudentProfile.FirstName} {p.StudentProfile.LastName}"
                            : "—",
                        AdmissionNumber = p.StudentProfile?.AdmissionNumber ?? "—",
                        ClassName = p.StudentProfile?.SchoolClass?.Name ?? "—",
                        AmountPaid = p.AmountPaid,
                        Gateway = p.Gateway,
                        Status = p.Status,
                        PaymentDate = p.PaymentDate,
                    })
                    .ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new FinanceDashboardDto
                {
                    AcademicSessionId = academicSessionId,
                    AcademicSessionName = session.Name,
                    TermId = termId,
                    TermName = term.Name,
                    TotalExpected = totalExpected,
                    TotalCollected = totalCollected,
                    TotalOutstanding = totalOutstanding,
                    CollectionRate = collectionRate,
                    TotalOnline = totalOnline,
                    TotalCash = totalCash,
                    TotalBankTransfer = totalBankTransfer,
                    TotalManual = totalManual,
                    TotalStudents = totalStudents,
                    FullyPaidCount = fullyPaid,
                    PartiallyPaidCount = partiallyPaid,
                    UnpaidCount = unpaid,
                    TotalTransactions = totalTx,
                    SuccessfulTransactions = successTx,
                    PendingTransactions = pendingTx,
                    FailedTransactions = failedTx,
                    ClassBreakdown = classBreakdown,
                    GatewayBreakdown = gatewayBreakdown,
                    RecentPayments = recent,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building finance dashboard.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // PAYMENTS LIST
        // ==========================================================
        #region GetPaymentsAsync
        public async Task<ServiceResponse<PaginatedViewModel<FinancePaymentRowDto>>> GetPaymentsAsync(
            FinancePaymentFilterDto filter)
        {
            var response = new ServiceResponse<PaginatedViewModel<FinancePaymentRowDto>>();

            try
            {
                filter.PageNumber = Math.Max(1, filter.PageNumber);
                filter.PageSize = Math.Max(1, Math.Min(100, filter.PageSize));

                var query = _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                        .ThenInclude(sp => sp.SchoolClass)
                    .AsQueryable();

                if (filter.AcademicSessionId.HasValue)
                    query = query.Where(p => p.AcademicSessionId == filter.AcademicSessionId.Value);

                if (filter.TermId.HasValue)
                    query = query.Where(p => p.TermId == filter.TermId.Value);

                if (filter.ClassId.HasValue)
                    query = query.Where(p => p.StudentProfile != null
                                          && p.StudentProfile.SchoolClassId == filter.ClassId.Value);

                if (filter.Gateway.HasValue)
                    query = query.Where(p => p.Gateway == filter.Gateway.Value);

                if (filter.Status.HasValue)
                    query = query.Where(p => p.Status == filter.Status.Value);

                if (filter.FromDate.HasValue)
                    query = query.Where(p => p.PaymentDate >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(p => p.PaymentDate <= filter.ToDate.Value);

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var s = filter.SearchTerm.Trim().ToLower();
                    query = query.Where(p =>
                        p.ReferenceNumber.ToLower().Contains(s) ||
                        (p.StudentProfile != null && (
                            p.StudentProfile.FirstName.ToLower().Contains(s) ||
                            p.StudentProfile.LastName.ToLower().Contains(s) ||
                            p.StudentProfile.AdmissionNumber.ToLower().Contains(s))));
                }

                int totalCount = await query.CountAsync();

                var rows = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(p => new FinancePaymentRowDto
                    {
                        Id = p.Id,
                        ReferenceNumber = p.ReferenceNumber,
                        StudentName = p.StudentProfile != null
                            ? p.StudentProfile.FirstName + " " + p.StudentProfile.LastName
                            : "—",
                        AdmissionNumber = p.StudentProfile != null
                            ? p.StudentProfile.AdmissionNumber
                            : "—",
                        ClassName = p.StudentProfile != null && p.StudentProfile.SchoolClass != null
                            ? p.StudentProfile.SchoolClass.Name
                            : "—",
                        AmountPaid = p.AmountPaid,
                        Gateway = p.Gateway,
                        Status = p.Status,
                        PaymentDate = p.PaymentDate,
                    })
                    .ToListAsync();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new PaginatedViewModel<FinancePaymentRowDto>
                {
                    Items = rows,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalCount > 0
                        ? (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                        : 0,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payments list.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // CASH PAYMENT — auto-creates / updates enrollment
        // ==========================================================
        #region RecordCashPaymentAsync
        public async Task<ServiceResponse<CashPaymentResponseDto>> RecordCashPaymentAsync(
            RecordCashPaymentDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<CashPaymentResponseDto>();

            try
            {
                if (request.AmountPaid <= 0)
                {
                    response.Success = false;
                    response.Message = "Amount must be greater than zero.";
                    response.StatusCode = 400;
                    return response;
                }

                var student = await _context.Students
                    .Include(s => s.SchoolClass)
                    .FirstOrDefaultAsync(s => s.Id == request.StudentProfileId);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- 1. Enrollment: lookup or create ----------
                var enrollment = await _context.StudentEnrollments
                    .FirstOrDefaultAsync(e =>
                        e.StudentProfileId == request.StudentProfileId &&
                        e.TermId == request.TermId &&
                        e.AcademicSessionId == request.AcademicSessionId);

                if (enrollment == null)
                {
                    var billedAmount = await _context.ClassFeePrices
                        .Include(cfp => cfp.Fee)
                        .Where(cfp => cfp.SchoolClassId == student.SchoolClassId &&
                                      cfp.Fee.TermId == request.TermId &&
                                      cfp.Fee.AcademicSessionId == request.AcademicSessionId)
                        .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                    enrollment = new StudentEnrollments
                    {
                        Id = Guid.NewGuid(),
                        StudentProfileId = student.Id,
                        AcademicSessionId = request.AcademicSessionId,
                        TermId = request.TermId,
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
                        student.Id, request.TermId, billedAmount);
                }

                // ---------- 2. Create the payment record ----------
                var reference = !string.IsNullOrWhiteSpace(request.ExternalReference)
                    ? request.ExternalReference.Trim()
                    : $"CASH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

                var payment = new StudentPayment
                {
                    Id = Guid.NewGuid(),
                    StudentProfileId = request.StudentProfileId,
                    AmountPaid = request.AmountPaid,
                    ReferenceNumber = reference,
                    Gateway = PaymentGateway.Cash,
                    Status = PaymentStatus.Successful,
                    AcademicSessionId = request.AcademicSessionId,
                    TermId = request.TermId,
                    EnrollmentId = enrollment.Id,       // ← set the FK (nullable but we know it here)
                    PaymentDate = DateTime.UtcNow,
                };

                _context.StudentPayments.Add(payment);

                // ---------- 3. Allocate to fees ----------
                if (request.Items != null && request.Items.Any())
                {
                    foreach (var item in request.Items)
                    {
                        payment.PaymentItems.Add(new StudentPaymentItem
                        {
                            Id = Guid.NewGuid(),
                            StudentPaymentId = payment.Id,
                            FeeId = item.FeeId,
                            AmountAllocated = item.AmountAllocated,
                        });
                    }
                }
                else
                {
                    // Auto-allocate: tuition first, else first available fee
                    var tuitionFee = await _context.Fees
                        .FirstOrDefaultAsync(f =>
                            f.SchoolClassId == enrollment.SchoolClassId &&
                            f.TermId == request.TermId &&
                            (f.FeeName.ToLower().Contains("tuition") ||
                             f.FeeName.ToLower().Contains("school fee")));

                    if (tuitionFee == null)
                    {
                        tuitionFee = await _context.Fees
                            .FirstOrDefaultAsync(f =>
                                f.SchoolClassId == enrollment.SchoolClassId &&
                                f.TermId == request.TermId);
                    }

                    if (tuitionFee != null)
                    {
                        payment.PaymentItems.Add(new StudentPaymentItem
                        {
                            Id = Guid.NewGuid(),
                            StudentPaymentId = payment.Id,
                            FeeId = tuitionFee.Id,
                            AmountAllocated = request.AmountPaid,
                        });
                    }
                }

                // ---------- 4. Recompute enrollment totals ----------
                var pastPaid = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == request.StudentProfileId &&
                                p.TermId == request.TermId &&
                                p.Status == PaymentStatus.Successful)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

                var newTotalPaid = pastPaid + request.AmountPaid;

                var totalRequiredFee = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId &&
                                  cfp.Fee.TermId == request.TermId)
                    .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                enrollment.TotalAmountPaid = newTotalPaid;
                enrollment.OutstandingBalance = Math.Max(0, totalRequiredFee - newTotalPaid);

                // ---------- 5. Status flags ----------
                var totalMinimum = await _context.ClassFeePrices
                    .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId &&
                                  cfp.Fee.TermId == request.TermId)
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

                enrollment.IsFullyPaid = totalRequiredFee > 0 && newTotalPaid >= totalRequiredFee;

                _context.StudentEnrollments.Update(enrollment);

                await _context.SaveChangesAsync();

                await _paymentLog.RecordPaymentAsync(
                    enrollment.StudentProfileId.ToString(),
                    request.AmountPaid,
                    reference,
                    PaymentGateway.Cash,
                    request.Note ?? "Cash payment recorded at bursary");

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Cash Payment Recorded",
                    $"Ref {reference}: ₦{request.AmountPaid:N2} for " +
                    $"{student.FirstName} {student.LastName} ({student.AdmissionNumber}).",
                    ipAddress);

                response.Success = true;
                response.StatusCode = 201;
                response.Message = "Cash payment recorded successfully.";
                response.Data = new CashPaymentResponseDto
                {
                    PaymentId = payment.Id,
                    ReferenceNumber = reference,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    AdmissionNumber = student.AdmissionNumber,
                    AmountPaid = request.AmountPaid,
                    NewOutstandingBalance = enrollment.OutstandingBalance,
                    IsFullyPaid = enrollment.IsFullyPaid,
                    RecordedAt = DateTime.UtcNow,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording cash payment.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // ARREARS
        // ==========================================================
        #region GetArrearsAsync
        public async Task<ServiceResponse<List<ArrearsRowDto>>> GetArrearsAsync(
            Guid academicSessionId, Guid termId, Guid? classId = null)
        {
            var response = new ServiceResponse<List<ArrearsRowDto>>();

            try
            {
                var query = _context.StudentEnrollments
                    .AsNoTracking()
                    .Include(e => e.StudentProfile)
                        .ThenInclude(sp => sp.SchoolClass)
                    .Where(e => e.TermId == termId
                             && e.AcademicSessionId == academicSessionId);

                if (classId.HasValue)
                    query = query.Where(e => e.SchoolClassId == classId.Value);

                var enrollments = await query.ToListAsync();

                var studentIds = enrollments.Select(e => e.StudentProfileId).ToList();

                var parentLinks = await _context.ParentStudents
                    .AsNoTracking()
                    .Include(ps => ps.Parent)
                    .Where(ps => studentIds.Contains(ps.StudentProfileId)
                              && ps.IsPrimaryContact)
                    .ToListAsync();

                var guardianPhoneByStudent = parentLinks
                    .GroupBy(ps => ps.StudentProfileId)
                    .ToDictionary(g => g.Key, g => g.First().Parent?.PhoneNumber ?? "");

                var rows = enrollments
                    .Select(e =>
                    {
                        var expected = e.TotalAmountPaid + e.OutstandingBalance;
                        return new ArrearsRowDto
                        {
                            StudentProfileId = e.StudentProfileId,
                            StudentName = $"{e.StudentProfile.FirstName} {e.StudentProfile.LastName}",
                            AdmissionNumber = e.StudentProfile.AdmissionNumber,
                            ClassName = e.SchoolClass?.Name ?? "—",
                            GuardianPhone = guardianPhoneByStudent.TryGetValue(e.StudentProfileId, out var p)
                                ? p
                                : "",
                            Expected = expected,
                            Paid = e.TotalAmountPaid,
                            Outstanding = e.OutstandingBalance,
                            IsFullyPaid = e.IsFullyPaid,
                        };
                    })
                    .Where(r => !r.IsFullyPaid)
                    .OrderByDescending(r => r.Outstanding)
                    .ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = rows;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching arrears.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion
    }
}