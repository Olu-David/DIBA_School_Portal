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
        private readonly INotificationLog _notification;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AccountService> _logger;

        private static readonly PaymentGateway[] OnlineGateways =
        {
            PaymentGateway.Flutterwave,
            PaymentGateway.Paystack,
        };

        // Who gets notified when the bursary records money
        private static readonly string[] FinanceNotificationRoles =
        {
            "SuperAdmin", "Principal", "VicePrincipal", "Accountant"
        };
        #endregion

        #region Constructor
        public AccountService(
            AppDbContext context,
            IAuditLog auditLog,
            IPaymentLog paymentLog,
            INotificationLog notification,
            IHttpContextAccessor http,
            ILogger<AccountService> logger)
        {
            _context = context;
            _auditLog = auditLog;
            _paymentLog = paymentLog;
            _notification = notification;
            _http = http;
            _logger = logger;
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        #endregion

        // ==========================================================
        // HELPERS
        // ==========================================================
        #region Notification helpers
        /// <summary>
        /// Fires a personal notification to every active user whose primary role
        /// is one of the finance-notification roles.
        /// </summary>
        private async Task NotifyFinanceStaffAsync(string title, string message)
        {
            try
            {
                var recipients = await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.IsActive
                        && !u.IsDeleted
                        && u.PrimaryRole != null
                        && FinanceNotificationRoles.Contains(u.PrimaryRole))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in recipients)
                {
                    await _notification.SendNotificationAsync(
                        userId,
                        title,
                        message,
                        category: "Finance");
                }
            }
            catch (Exception ex)
            {
                // Never let a notification failure break the payment flow
                _logger.LogWarning(ex, "Failed to dispatch finance notification.");
            }
        }

        /// <summary>
        /// Fires a notification to a single user if their ID is non-empty.
        /// </summary>
        private async Task NotifyUserSafeAsync(
            string? userId, string title, string message, string category = "Finance")
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                await _notification.SendNotificationAsync(
                    userId, title, message, category);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Failed to send notification to user {UserId}.", userId);
            }
        }
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

                // ---------- 2. Aggregate enrollments ----------
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
                    })
                    .FirstOrDefaultAsync();

                int totalStudents = enrollmentStats?.TotalStudents ?? 0;
                int fullyPaid = enrollmentStats?.FullyPaid ?? 0;
                int unpaid = enrollmentStats?.Unpaid ?? 0;
                int partiallyPaid = totalStudents - fullyPaid - unpaid;
                decimal totalExpected = enrollmentStats?.TotalExpected ?? 0m;

                // ---------- 3. Fee payments ----------
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
                decimal totalOutstanding = Math.Max(0, totalExpected - totalCollected);
                decimal collectionRate = totalExpected > 0
                    ? Math.Round(totalCollected / totalExpected * 100, 2)
                    : 0m;

                // ---------- 4. Misc income ----------
                var miscStats = await _context.MiscellaneousIncomes
                    .AsNoTracking()
                    .Where(m => m.TermId == termId
                             && m.AcademicSessionId == academicSessionId
                             && m.Status == PaymentStatus.Successful)
                    .GroupBy(m => m.Category)
                    .Select(g => new MiscIncomeCategorySummaryDto
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(x => x.Amount),
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToListAsync();

                decimal totalMiscIncome = miscStats.Sum(x => x.TotalAmount);
                int miscTxCount = miscStats.Sum(x => x.Count);

                // ---------- 5. EXPENSES — totals + category + gateway ----------
                var expensesForTerm = _context.Expenses
                    .AsNoTracking()
                    .Where(e => !e.IsDeleted
                             && e.AcademicSessionId == academicSessionId
                             && e.TermId == termId);

                var expenseStats = await expensesForTerm
                    .GroupBy(e => 1)
                    .Select(g => new
                    {
                        TotalExpenses = g.Sum(e => e.Amount),
                        Count = g.Count(),
                    })
                    .FirstOrDefaultAsync();

                decimal totalExpenses = expenseStats?.TotalExpenses ?? 0m;
                int expenseCount = expenseStats?.Count ?? 0;

                var expensesByCategory = await expensesForTerm
                    .GroupBy(e => e.Category)
                    .Select(g => new ExpenseCategorySummaryDto
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(e => e.Amount),
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToListAsync();

                var expensesByGateway = await expensesForTerm
                    .GroupBy(e => e.PaymentGateway)
                    .Select(g => new ExpenseGatewaySummaryDto
                    {
                        Gateway = g.Key,
                        GatewayName = g.Key.ToString(),
                        Count = g.Count(),
                        TotalAmount = g.Sum(e => e.Amount),
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToListAsync();

                // ---------- 6. Derived totals ----------
                decimal totalIncome = totalCollected + totalMiscIncome;
                decimal netInflow = totalIncome - totalExpenses;

                // ---------- 7. Class breakdown ----------
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

                foreach (var c in classBreakdown)
                    c.CollectionRate = c.Expected > 0
                        ? Math.Round(c.Collected / c.Expected * 100, 2)
                        : 0m;

                // ---------- 8. Recent fee payments ----------
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

                // ---------- 9. Fee transaction counts ----------
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

                // ---------- 10. Build response ----------
                response.Success = true;
                response.StatusCode = 200;
                response.Data = new FinanceDashboardDto
                {
                    AcademicSessionId = academicSessionId,
                    AcademicSessionName = ctx.Name,
                    TermId = termId,
                    TermName = ctx.Term.Name,

                    // Fee collection
                    TotalExpected = totalExpected,
                    TotalCollected = totalCollected,
                    TotalOutstanding = totalOutstanding,
                    CollectionRate = collectionRate,

                    // Fee payment methods
                    TotalOnline = paymentStats?.Online ?? 0m,
                    TotalCash = paymentStats?.Cash ?? 0m,
                    TotalBankTransfer = paymentStats?.Bank ?? 0m,
                    TotalManual = paymentStats?.Manual ?? 0m,

                    // Student fee status
                    TotalStudents = totalStudents,
                    FullyPaidCount = fullyPaid,
                    PartiallyPaidCount = partiallyPaid,
                    UnpaidCount = unpaid,

                    // Fee transaction counts
                    TotalTransactions = txCounts?.Total ?? 0,
                    SuccessfulTransactions = txCounts?.Success ?? 0,
                    PendingTransactions = txCounts?.Pending ?? 0,
                    FailedTransactions = txCounts?.Failed ?? 0,

                    // Breakdowns
                    ClassBreakdown = classBreakdown,
                    GatewayBreakdown = new List<GatewaySummaryDto>(),
                    RecentPayments = recent,

                    // Misc income
                    TotalMiscIncome = totalMiscIncome,
                    MiscIncomeTransactionCount = miscTxCount,
                    MiscIncomeByCategory = miscStats,

                    // Income total
                    TotalIncome = totalIncome,

                    // Expenses
                    TotalExpenses = totalExpenses,
                    ExpenseTransactionCount = expenseCount,
                    ExpensesByCategory = expensesByCategory,
                    ExpensesByGateway = expensesByGateway,

                    // Net
                    NetInflow = netInflow,

                    // Legacy
                    GrandTotalInflow = totalIncome,
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

                // ---------- 1a. GUARD: only school fees touch enrollment ----------
                // If the student's class has no fee schedule for this term,
                // this isn't a school-fee payment — it's PTA/uniform/fine/etc.
                // Those must go through the Other Income tab, not here.
                var totalBilledForTerm = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .Where(cfp => cfp.SchoolClassId == student.SchoolClassId &&
                                  cfp.Fee.TermId == request.TermId &&
                                  cfp.Fee.AcademicSessionId == request.AcademicSessionId)
                    .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                if (totalBilledForTerm <= 0)
                {
                    response.Success = false;
                    response.Message =
                        "This student has no school fees configured for this term. " +
                        "If this payment is for PTA, uniform, exam fee, or other non-tuition items, " +
                        "use the Other Income tab instead.";
                    response.StatusCode = 400;
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
                    enrollment = new StudentEnrollment
                    {
                        Id = Guid.NewGuid(),
                        StudentProfileId = student.Id,
                        AcademicSessionId = request.AcademicSessionId,
                        TermId = request.TermId,
                        SchoolClassId = student.SchoolClassId,

                        TotalAmountPaid = 0m,
                        OutstandingBalance = totalBilledForTerm,
                        IsFullyPaid = false,

                        Status = StudentEnrollmentStatus.Pending,
                        IsActive = false,
                        EnrolledAt = DateTime.UtcNow,
                    };

                    _context.StudentEnrollments.Add(enrollment);

                    _logger.LogInformation(
                        "Auto-created enrollment for student {StudentId} term {TermId} — billed ₦{Billed:N2}.",
                        student.Id, request.TermId, totalBilledForTerm);
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
                    EnrollmentId = enrollment.Id,
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

                // ---------- 4. Update enrollment totals (SUBTRACT, don't recalculate) ----------
                enrollment.TotalAmountPaid += request.AmountPaid;
                enrollment.OutstandingBalance = Math.Max(0, enrollment.OutstandingBalance - request.AmountPaid);
                enrollment.IsFullyPaid = enrollment.OutstandingBalance <= 0;

                // ---------- 5. Status flags ----------
                var totalMinimum = await _context.ClassFeePrices
                    .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId &&
                                  cfp.Fee.TermId == request.TermId)
                    .SumAsync(cfp => (decimal?)cfp.MinimumAmountToPay) ?? 0m;

                if (totalMinimum > 0)
                {
                    if (enrollment.TotalAmountPaid < totalMinimum)
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

                await _context.SaveChangesAsync();

                // ---------- 6. Logs ----------
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

                // ---------- 7. Notify finance staff ----------
                await NotifyFinanceStaffAsync(
                    "Cash payment recorded",
                    $"₦{request.AmountPaid:N2} received from " +
                    $"{student.FirstName} {student.LastName} " +
                    $"({student.AdmissionNumber}). Ref {reference}.");

                // ---------- 8. Notify student ----------
                await NotifyUserSafeAsync(
                    student.UserId,
                    "Payment received",
                    $"Your payment of ₦{request.AmountPaid:N2} has been recorded. " +
                    $"Outstanding balance: ₦{enrollment.OutstandingBalance:N2}.",
                    category: "Finance");

                // ---------- 9. Notify linked parents ----------
                var parentIds = await _context.ParentStudents
                    .AsNoTracking()
                    .Where(ps => ps.StudentProfileId == student.Id)
                    .Select(ps => ps.ParentUserId)
                    .ToListAsync();

                foreach (var parentId in parentIds)
                {
                    await NotifyUserSafeAsync(
                        parentId,
                        "Payment recorded for your child",
                        $"₦{request.AmountPaid:N2} received for " +
                        $"{student.FirstName} {student.LastName}. " +
                        $"Outstanding balance: ₦{enrollment.OutstandingBalance:N2}.",
                        category: "Finance");
                }

                // ---------- 10. Response ----------
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
        // SET OUTSTANDING — bursar sets the student's starting balance
        // ==========================================================
        #region SetOutstandingAsync
        public async Task<ServiceResponse<decimal>> SetOutstandingAsync(
            Guid studentProfileId,
            Guid academicSessionId,
            Guid termId,
            decimal amount,
            string currentUserId,
            string ipAddress)
        {
            var response = new ServiceResponse<decimal>();

            try
            {
                if (amount < 0)
                {
                    response.Success = false;
                    response.Message = "Outstanding amount cannot be negative.";
                    response.StatusCode = 400;
                    return response;
                }

                var student = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == studentProfileId);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var enrollment = await _context.StudentEnrollments
                    .FirstOrDefaultAsync(e =>
                        e.StudentProfileId == studentProfileId &&
                        e.TermId == termId &&
                        e.AcademicSessionId == academicSessionId);

                if (enrollment == null)
                {
                    // No enrollment yet — create one with this starting balance
                    enrollment = new StudentEnrollment
                    {
                        Id = Guid.NewGuid(),
                        StudentProfileId = studentProfileId,
                        AcademicSessionId = academicSessionId,
                        TermId = termId,
                        SchoolClassId = student.SchoolClassId,
                        TotalAmountPaid = 0m,
                        OutstandingBalance = amount,
                        IsFullyPaid = amount == 0m,
                        Status = amount == 0m
                            ? StudentEnrollmentStatus.Active
                            : StudentEnrollmentStatus.Pending,
                        IsActive = amount == 0m,
                        EnrolledAt = DateTime.UtcNow,
                    };
                    _context.StudentEnrollments.Add(enrollment);
                }
                else
                {
                    // Existing — just set the balance directly
                    enrollment.OutstandingBalance = amount;
                    enrollment.IsFullyPaid = amount == 0m;

                    if (amount == 0m)
                    {
                        enrollment.Status = StudentEnrollmentStatus.Active;
                        enrollment.IsActive = true;
                    }
                    else
                    {
                        enrollment.Status = StudentEnrollmentStatus.Pending;
                        enrollment.IsActive = false;
                    }

                    _context.StudentEnrollments.Update(enrollment);
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Outstanding set",
                    $"Student {studentProfileId}: outstanding set to ₦{amount:N2}.",
                    ipAddress);

                response.Success = true;
                response.StatusCode = 200;
                response.Message = $"Outstanding set to ₦{amount:N2}.";
                response.Data = enrollment.OutstandingBalance;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting outstanding.");
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

        // ==========================================================
        // MISC INCOME — record cash outside the fee schedule
        // ==========================================================
        #region RecordMiscIncomeAsync
        public async Task<ServiceResponse<MiscIncomeResponseDto>> RecordMiscIncomeAsync(
            RecordMiscIncomeDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<MiscIncomeResponseDto>();

            try
            {
                // ---------- 1. Validate amount ----------
                if (request.Amount <= 0)
                {
                    response.Success = false;
                    response.Message = "Amount must be greater than zero.";
                    response.StatusCode = 400;
                    return response;
                }

                // ---------- 2. Validate category + description ----------
                if (string.IsNullOrWhiteSpace(request.Category))
                {
                    response.Success = false;
                    response.Message = "Category is required.";
                    response.StatusCode = 400;
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    response.Success = false;
                    response.Message = "Description is required.";
                    response.StatusCode = 400;
                    return response;
                }

                // ---------- 3. Validate session + term ----------
                var sessionTerm = await _context.AcademicSessions
                    .AsNoTracking()
                    .Where(s => s.Id == request.AcademicSessionId)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        Term = _context.SchoolTerms
                            .AsNoTracking()
                            .Where(t => t.Id == request.TermId
                                     && t.AcademicSessionId == s.Id)
                            .Select(t => new { t.Id, t.Name })
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (sessionTerm == null)
                {
                    response.Success = false;
                    response.Message = "Academic session not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (sessionTerm.Term == null)
                {
                    response.Success = false;
                    response.Message = "Term not found or does not belong to session.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- 4. Optional: validate student link ----------
                string? studentName = null;
                if (request.StudentProfileId.HasValue)
                {
                    var student = await _context.Students
                        .AsNoTracking()
                        .Where(s => s.Id == request.StudentProfileId.Value)
                        .Select(s => new { s.FirstName, s.LastName })
                        .FirstOrDefaultAsync();

                    if (student == null)
                    {
                        response.Success = false;
                        response.Message = "Student not found.";
                        response.StatusCode = 404;
                        return response;
                    }

                    studentName = $"{student.FirstName} {student.LastName}";
                }

                // ---------- 5. Generate reference ----------
                var reference = !string.IsNullOrWhiteSpace(request.ExternalReference)
                    ? request.ExternalReference.Trim()
                    : $"MISC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

                // ---------- 6. Create the record ----------
                var income = new MiscellaneousIncome
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = reference,
                    Category = request.Category.Trim(),
                    Description = request.Description.Trim(),
                    Amount = request.Amount,
                    PayerName = string.IsNullOrWhiteSpace(request.PayerName)
                        ? null : request.PayerName.Trim(),
                    PayerPhone = string.IsNullOrWhiteSpace(request.PayerPhone)
                        ? null : request.PayerPhone.Trim(),
                    StudentProfileId = request.StudentProfileId,
                    AcademicSessionId = request.AcademicSessionId,
                    TermId = request.TermId,
                    Gateway = request.Gateway,
                    Status = PaymentStatus.Successful,
                    RecordedByUserId = currentUserId,
                    Note = string.IsNullOrWhiteSpace(request.Note)
                        ? null : request.Note.Trim(),
                    RecordedAt = DateTime.UtcNow,
                };

                _context.MiscellaneousIncomes.Add(income);
                await _context.SaveChangesAsync();

                // ---------- 7. Logs ----------
                await _paymentLog.RecordPaymentAsync(
                    currentUserId,
                    request.Amount,
                    reference,
                    request.Gateway,
                    $"Misc income — {income.Category}: {income.Description}");

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Misc Income Recorded",
                    $"Ref {reference}: ₦{request.Amount:N2} ({income.Category}) — " +
                    $"{income.Description}" +
                    (studentName != null ? $" [Student: {studentName}]" : ""),
                    ipAddress);

                // ---------- 8. Notify finance staff ----------
                await NotifyFinanceStaffAsync(
                    "Income recorded",
                    $"₦{request.Amount:N2} — {income.Category}: {income.Description}" +
                    (studentName != null ? $" ({studentName})" : "") +
                    $". Ref {reference}.");

                // ---------- 9. If linked to a student, notify them + parents ----------
                if (request.StudentProfileId.HasValue)
                {
                    var studentUserId = await _context.Students
                        .AsNoTracking()
                        .Where(s => s.Id == request.StudentProfileId.Value)
                        .Select(s => s.UserId)
                        .FirstOrDefaultAsync();

                    await NotifyUserSafeAsync(
                        studentUserId,
                        "Payment recorded",
                        $"₦{request.Amount:N2} recorded for {income.Category}: " +
                        $"{income.Description}. Ref {reference}.",
                        category: "Finance");

                    var parentIds = await _context.ParentStudents
                        .AsNoTracking()
                        .Where(ps => ps.StudentProfileId == request.StudentProfileId.Value)
                        .Select(ps => ps.ParentUserId)
                        .ToListAsync();

                    foreach (var parentId in parentIds)
                    {
                        await NotifyUserSafeAsync(
                            parentId,
                            "Payment recorded for your child",
                            $"₦{request.Amount:N2} — {income.Category}: " +
                            $"{income.Description}. Ref {reference}.",
                            category: "Finance");
                    }
                }

                // ---------- 10. Response ----------
                response.Success = true;
                response.StatusCode = 201;
                response.Message = "Income recorded successfully.";
                response.Data = new MiscIncomeResponseDto
                {
                    Id = income.Id,
                    ReferenceNumber = income.ReferenceNumber,
                    Category = income.Category,
                    Description = income.Description,
                    Amount = income.Amount,
                    PayerName = income.PayerName,
                    StudentProfileId = income.StudentProfileId,
                    StudentName = studentName,
                    Gateway = income.Gateway,
                    Status = income.Status,
                    RecordedAt = income.RecordedAt,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording misc income.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // MISC INCOME — list with filters
        // ==========================================================
        #region GetMiscIncomeAsync
        public async Task<ServiceResponse<PaginatedViewModel<MiscIncomeRowDto>>> GetMiscIncomeAsync(
            MiscIncomeFilterDto filter)
        {
            var response = new ServiceResponse<PaginatedViewModel<MiscIncomeRowDto>>();

            try
            {
                filter.PageNumber = Math.Max(1, filter.PageNumber);
                filter.PageSize = Math.Max(1, Math.Min(100, filter.PageSize));

                var query = _context.MiscellaneousIncomes
                    .AsNoTracking()
                    .Include(m => m.StudentProfile)
                        .ThenInclude(s => s!.SchoolClass)
                    .AsQueryable();

                if (filter.AcademicSessionId.HasValue)
                    query = query.Where(m => m.AcademicSessionId == filter.AcademicSessionId.Value);

                if (filter.TermId.HasValue)
                    query = query.Where(m => m.TermId == filter.TermId.Value);

                if (!string.IsNullOrWhiteSpace(filter.Category))
                    query = query.Where(m => m.Category == filter.Category);

                if (filter.Gateway.HasValue)
                    query = query.Where(m => m.Gateway == filter.Gateway.Value);

                if (filter.FromDate.HasValue)
                    query = query.Where(m => m.RecordedAt >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(m => m.RecordedAt <= filter.ToDate.Value);

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var s = filter.SearchTerm.Trim().ToLower();
                    query = query.Where(m =>
                        m.ReferenceNumber.ToLower().Contains(s) ||
                        m.Category.ToLower().Contains(s) ||
                        m.Description.ToLower().Contains(s) ||
                        (m.PayerName != null && m.PayerName.ToLower().Contains(s)) ||
                        (m.StudentProfile != null && (
                            m.StudentProfile.FirstName.ToLower().Contains(s) ||
                            m.StudentProfile.LastName.ToLower().Contains(s) ||
                            m.StudentProfile.AdmissionNumber.ToLower().Contains(s))));
                }

                int totalCount = await query.CountAsync();

                var rows = await query
                    .OrderByDescending(m => m.RecordedAt)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(m => new MiscIncomeRowDto
                    {
                        Id = m.Id,
                        ReferenceNumber = m.ReferenceNumber,
                        Category = m.Category,
                        Description = m.Description,
                        Amount = m.Amount,
                        PayerName = m.PayerName,
                        StudentName = m.StudentProfile != null
                            ? m.StudentProfile.FirstName + " " + m.StudentProfile.LastName
                            : null,
                        ClassName = m.StudentProfile != null && m.StudentProfile.SchoolClass != null
                            ? m.StudentProfile.SchoolClass.Name
                            : null,
                        Gateway = m.Gateway,
                        Status = m.Status,
                        RecordedAt = m.RecordedAt,
                    })
                    .ToListAsync();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new PaginatedViewModel<MiscIncomeRowDto>
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
                _logger.LogError(ex, "Error fetching misc income.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion
    }
}