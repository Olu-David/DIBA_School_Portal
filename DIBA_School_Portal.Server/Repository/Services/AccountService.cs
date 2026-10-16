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
                // ---------- 1. Validate session + term in ONE query ----------
                var ctx = await _context.AcademicSessions
                    .AsNoTracking()
                    .Where(s => s.Id == academicSessionId)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        Term = _context.SchoolTerms
                            .AsNoTracking()
                            .Where(t => t.Id == termId && t.AcademicSessionId == s.Id)
                            .Select(t => new { t.Id, t.Name })
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (ctx == null)
                {
                    response.Success = false;
                    response.Message = "Academic session not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (ctx.Term == null)
                {
                    response.Success = false;
                    response.Message = "Term not found or does not belong to session.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- 2. Aggregate enrollments in SQL (no loading rows into memory) ----------
                var enrollmentStats = await _context.StudentEnrollments
                    .AsNoTracking()
                    .Where(e => e.TermId == termId && e.AcademicSessionId == academicSessionId)
                    .GroupBy(e => 1)
                    .Select(g => new
                    {
                        TotalStudents = g.Count(),
                        FullyPaid = g.Count(e => e.IsFullyPaid),
                        Unpaid = g.Count(e => e.TotalAmountPaid == 0),
                        TotalExpected = g.Sum(e => e.TotalAmountPaid + e.OutstandingBalance),
                        TotalPaid = g.Sum(e => e.TotalAmountPaid),
                    })
                    .FirstOrDefaultAsync();

                if (enrollmentStats == null)
                {
                    // no enrollments — return an empty but valid dashboard
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Data = new FinanceDashboardDto
                    {
                        AcademicSessionId = academicSessionId,
                        AcademicSessionName = ctx.Name,
                        TermId = termId,
                        TermName = ctx.Term.Name,
                        ClassBreakdown = new(),
                        GatewayBreakdown = new(),
                        RecentPayments = new(),
                    };
                    return response;
                }

                // ---------- 3. Aggregate payments in SQL ----------
                var paymentStats = await _context.StudentPayments
                    .AsNoTracking()
                    .Where(p => p.TermId == termId
                             && p.AcademicSessionId == academicSessionId
                             && p.Status == PaymentStatus.Successful)
                    .GroupBy(p => 1)
                    .Select(g => new
                    {
                        TotalCollected = g.Sum(p => p.AmountPaid),
                        Online = g.Where(p => p.Gateway == PaymentGateway.Flutterwave
                                            || p.Gateway == PaymentGateway.Paystack)
                                   .Sum(p => (decimal?)p.AmountPaid) ?? 0m,
                        Cash = g.Where(p => p.Gateway == PaymentGateway.Cash)
                                   .Sum(p => (decimal?)p.AmountPaid) ?? 0m,
                        Bank = g.Where(p => p.Gateway == PaymentGateway.BankTransfer)
                                   .Sum(p => (decimal?)p.AmountPaid) ?? 0m,
                        Manual = g.Where(p => p.Gateway == PaymentGateway.Manual)
                                   .Sum(p => (decimal?)p.AmountPaid) ?? 0m,
                    })
                    .FirstOrDefaultAsync();

                decimal totalCollected = paymentStats?.TotalCollected ?? 0m;
                decimal totalExpected = enrollmentStats.TotalExpected;
                decimal totalOutstanding = Math.Max(0, totalExpected - totalCollected);
                decimal collectionRate = totalExpected > 0
                    ? Math.Round(totalCollected / totalExpected * 100, 2)
                    : 0m;

                // ---------- 4. Class breakdown in SQL ----------
                var classBreakdown = await _context.StudentEnrollments
                    .AsNoTracking()
                    .Where(e => e.TermId == termId && e.AcademicSessionId == academicSessionId)
                    .GroupBy(e => new { e.SchoolClassId, e.SchoolClass!.Name })
                    .Select(g => new ClassFinanceSummaryDto
                    {
                        ClassId = g.Key.SchoolClassId,
                        ClassName = g.Key.Name ?? "—",
                        StudentCount = g.Count(),
                        Expected = g.Sum(e => e.TotalAmountPaid + e.OutstandingBalance),
                        Collected = g.Sum(e => e.TotalAmountPaid),
                        Outstanding = g.Sum(e => e.OutstandingBalance),
                        FullyPaidCount = g.Count(e => e.IsFullyPaid),
                        UnpaidCount = g.Count(e => e.TotalAmountPaid == 0),
                    })
                    .OrderBy(c => c.ClassName)
                    .ToListAsync();

                // compute rate client-side to avoid the SQL division on nullable
                foreach (var c in classBreakdown)
                    c.CollectionRate = c.Expected > 0
                        ? Math.Round(c.Collected / c.Expected * 100, 2)
                        : 0m;

                // ---------- 5. Recent payments (projected, top 10 only) ----------
                var recent = await _context.StudentPayments
                    .AsNoTracking()
                    .Where(p => p.TermId == termId && p.AcademicSessionId == academicSessionId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .Select(p => new FinancePaymentRowDto
                    {
                        Id = p.Id,
                        ReferenceNumber = p.ReferenceNumber,
                        StudentName = p.StudentProfile != null
                                            ? p.StudentProfile.FirstName + " " + p.StudentProfile.LastName
                                            : "—",
                        AdmissionNumber = p.StudentProfile != null ? p.StudentProfile.AdmissionNumber : "—",
                        ClassName = p.StudentProfile != null && p.StudentProfile.SchoolClass != null
                                            ? p.StudentProfile.SchoolClass.Name
                                            : "—",
                        AmountPaid = p.AmountPaid,
                        Gateway = p.Gateway,
                        Status = p.Status,
                        PaymentDate = p.PaymentDate,
                    })
                    .ToListAsync();

                // ---------- 6. Transaction counts (single aggregate) ----------
                var txCounts = await _context.StudentPayments
                    .AsNoTracking()
                    .Where(p => p.TermId == termId && p.AcademicSessionId == academicSessionId)
                    .GroupBy(p => 1)
                    .Select(g => new
                    {
                        Total = g.Count(),
                        Success = g.Count(p => p.Status == PaymentStatus.Successful),
                        Pending = g.Count(p => p.Status == PaymentStatus.Pending),
                        Failed = g.Count(p => p.Status == PaymentStatus.Failed),
                    })
                    .FirstOrDefaultAsync();

                int partiallyPaid = enrollmentStats.TotalStudents
                                  - enrollmentStats.FullyPaid
                                  - enrollmentStats.Unpaid;

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new FinanceDashboardDto
                {
                    AcademicSessionId = academicSessionId,
                    AcademicSessionName = ctx.Name,
                    TermId = termId,
                    TermName = ctx.Term.Name,

                    TotalExpected = totalExpected,
                    TotalCollected = totalCollected,
                    TotalOutstanding = totalOutstanding,
                    CollectionRate = collectionRate,

                    TotalOnline = paymentStats?.Online ?? 0m,
                    TotalCash = paymentStats?.Cash ?? 0m,
                    TotalBankTransfer = paymentStats?.Bank ?? 0m,
                    TotalManual = paymentStats?.Manual ?? 0m,

                    TotalStudents = enrollmentStats.TotalStudents,
                    FullyPaidCount = enrollmentStats.FullyPaid,
                    PartiallyPaidCount = partiallyPaid,
                    UnpaidCount = enrollmentStats.Unpaid,

                    TotalTransactions = txCounts?.Total ?? 0,
                    SuccessfulTransactions = txCounts?.Success ?? 0,
                    PendingTransactions = txCounts?.Pending ?? 0,
                    FailedTransactions = txCounts?.Failed ?? 0,

                    ClassBreakdown = classBreakdown,
                    GatewayBreakdown = new List<GatewaySummaryDto>(),  // compute if needed
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