
 using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ExpenseDtos;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Repository.Services
    {
        public class ExpenseService : IExpenseService
        {
            private readonly AppDbContext _context;
            private readonly IAuditLog _auditLog;
            private readonly ILogger<ExpenseService> _logger;

            public ExpenseService(
                AppDbContext context,
                IAuditLog auditLog,
                ILogger<ExpenseService> logger)
            {
                _context = context;
                _auditLog = auditLog;
                _logger = logger;
            }

            // ==========================================================
            // LIST
            // ==========================================================
            public async Task<ServiceResponse<PaginatedViewModel<ExpenseRowDto>>> GetExpensesAsync(
                ExpenseFilterDto filter)
            {
                var response = new ServiceResponse<PaginatedViewModel<ExpenseRowDto>>();

                try
                {
                    filter.PageNumber = Math.Max(1, filter.PageNumber);
                    filter.PageSize = Math.Max(1, Math.Min(100, filter.PageSize));

                    var query = _context.Expenses
                        .AsNoTracking()
                        .Where(e => !e.IsDeleted)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filter.Category))
                        query = query.Where(e => e.Category == filter.Category);

                    // 👈 new gateway filter
                    if (filter.PaymentGateway.HasValue)
                        query = query.Where(e => e.PaymentGateway == filter.PaymentGateway.Value);

                    if (filter.FromDate.HasValue)
                        query = query.Where(e => e.ExpenseDate >= filter.FromDate.Value);

                    if (filter.ToDate.HasValue)
                        query = query.Where(e => e.ExpenseDate <= filter.ToDate.Value);

                    if (filter.AcademicSessionId.HasValue)
                        query = query.Where(e => e.AcademicSessionId == filter.AcademicSessionId.Value);

                    if (filter.TermId.HasValue)
                        query = query.Where(e => e.TermId == filter.TermId.Value);

                    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                    {
                        var s = filter.SearchTerm.Trim().ToLower();
                        query = query.Where(e =>
                            e.Description.ToLower().Contains(s) ||
                            e.Category.ToLower().Contains(s) ||
                            (e.Vendor != null && e.Vendor.ToLower().Contains(s)) ||
                            e.ReferenceNumber.ToLower().Contains(s));
                    }

                    int totalCount = await query.CountAsync();

                    var expenses = await query
                        .OrderByDescending(e => e.ExpenseDate)
                        .ThenByDescending(e => e.CreatedAt)
                        .Skip((filter.PageNumber - 1) * filter.PageSize)
                        .Take(filter.PageSize)
                        .ToListAsync();

                    var userIds = expenses
                        .Select(e => e.RecordedByUserId)
                        .Distinct()
                        .ToList();

                    var userMap = await _context.Users
                        .Where(u => userIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.FirstName, u.LastName })
                        .ToDictionaryAsync(
                            u => u.Id,
                            u => $"{u.FirstName} {u.LastName}".Trim());

                    var rows = expenses.Select(e => new ExpenseRowDto
                    {
                        Id = e.Id,
                        Category = e.Category,
                        Description = e.Description,
                        Amount = e.Amount,
                        PaymentGateway = e.PaymentGateway,
                        GatewayName = e.PaymentGateway.ToString(),
                        Vendor = e.Vendor,
                        ReferenceNumber = e.ReferenceNumber,
                        ExpenseDate = e.ExpenseDate,
                        ReceiptUrl = e.ReceiptUrl,
                        Notes = e.Notes,
                        RecordedByName = userMap.TryGetValue(e.RecordedByUserId, out var rn) ? rn : null,
                        CreatedAt = e.CreatedAt,
                    }).ToList();

                    response.Success = true;
                    response.StatusCode = 200;
                    response.Data = new PaginatedViewModel<ExpenseRowDto>
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
                    _logger.LogError(ex, "Error fetching expenses.");
                    response.Success = false;
                    response.Message = $"An error occurred: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            }

            // ==========================================================
            // GET BY ID
            // ==========================================================
            public async Task<ServiceResponse<ExpenseRowDto>> GetExpenseByIdAsync(Guid id)
            {
                var response = new ServiceResponse<ExpenseRowDto>();

                try
                {
                    var e = await _context.Expenses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                    if (e == null)
                    {
                        response.Success = false;
                        response.Message = "Expense not found.";
                        response.StatusCode = 404;
                        return response;
                    }

                    var userMap = await _context.Users
                        .Where(u => u.Id == e.RecordedByUserId)
                        .Select(u => new { u.Id, u.FirstName, u.LastName })
                        .ToDictionaryAsync(
                            u => u.Id,
                            u => $"{u.FirstName} {u.LastName}".Trim());

                    response.Success = true;
                    response.StatusCode = 200;
                    response.Data = new ExpenseRowDto
                    {
                        Id = e.Id,
                        Category = e.Category,
                        Description = e.Description,
                        Amount = e.Amount,
                        PaymentGateway = e.PaymentGateway,
                        GatewayName = e.PaymentGateway.ToString(),
                        Vendor = e.Vendor,
                        ReferenceNumber = e.ReferenceNumber,
                        ExpenseDate = e.ExpenseDate,
                        ReceiptUrl = e.ReceiptUrl,
                        Notes = e.Notes,
                        RecordedByName = userMap.TryGetValue(e.RecordedByUserId, out var rn) ? rn : null,
                        CreatedAt = e.CreatedAt,
                    };
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching expense {Id}.", id);
                    response.Success = false;
                    response.Message = $"An error occurred: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            }

            // ==========================================================
            // CREATE
            // ==========================================================
            public async Task<ServiceResponse<ExpenseRowDto>> CreateExpenseAsync(
                CreateExpenseDto request, string currentUserId, string ipAddress)
            {
                var response = new ServiceResponse<ExpenseRowDto>();

                try
                {
                    if (request.Amount <= 0)
                    {
                        response.Success = false;
                        response.Message = "Amount must be greater than zero.";
                        response.StatusCode = 400;
                        return response;
                    }

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

                    var reference = await GenerateReferenceNumberAsync();

                    var expense = new Expense
                    {
                        Id = Guid.NewGuid(),
                        Category = request.Category.Trim(),
                        Description = request.Description.Trim(),
                        Amount = request.Amount,
                        PaymentGateway = request.PaymentGateway,   // 👈 enum
                        Vendor = string.IsNullOrWhiteSpace(request.Vendor)
                            ? null
                            : request.Vendor.Trim(),
                        ReferenceNumber = reference,
                        ExpenseDate = request.ExpenseDate ?? DateTime.UtcNow,
                        Notes = string.IsNullOrWhiteSpace(request.Notes)
                            ? null
                            : request.Notes.Trim(),
                        RecordedByUserId = currentUserId,
                        AcademicSessionId = request.AcademicSessionId,
                        TermId = request.TermId,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                    };

                    _context.Expenses.Add(expense);
                    await _context.SaveChangesAsync();

                    await _auditLog.LogActivityAsync(
                        currentUserId,
                        "Expense Recorded",
                        $"Ref {expense.ReferenceNumber}: {expense.Category} — ₦{expense.Amount:N2} ({expense.PaymentGateway}).",
                        ipAddress);

                    return await GetExpenseByIdAsync(expense.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating expense.");
                    response.Success = false;
                    response.Message = $"An error occurred: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            }

            // ==========================================================
            // UPDATE
            // ==========================================================
            public async Task<ServiceResponse<ExpenseRowDto>> UpdateExpenseAsync(
                Guid id, UpdateExpenseDto request, string currentUserId, string ipAddress)
            {
                var response = new ServiceResponse<ExpenseRowDto>();

                try
                {
                    if (request.Amount <= 0)
                    {
                        response.Success = false;
                        response.Message = "Amount must be greater than zero.";
                        response.StatusCode = 400;
                        return response;
                    }

                    var expense = await _context.Expenses
                        .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                    if (expense == null)
                    {
                        response.Success = false;
                        response.Message = "Expense not found.";
                        response.StatusCode = 404;
                        return response;
                    }

                    expense.Category = request.Category.Trim();
                    expense.Description = request.Description.Trim();
                    expense.Amount = request.Amount;
                    expense.PaymentGateway = request.PaymentGateway;   // 👈 enum
                    expense.Vendor = string.IsNullOrWhiteSpace(request.Vendor)
                        ? null
                        : request.Vendor.Trim();
                    expense.ExpenseDate = request.ExpenseDate ?? expense.ExpenseDate;
                    expense.Notes = string.IsNullOrWhiteSpace(request.Notes)
                        ? null
                        : request.Notes.Trim();
                    expense.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    await _auditLog.LogActivityAsync(
                        currentUserId,
                        "Expense Updated",
                        $"Ref {expense.ReferenceNumber}: ₦{expense.Amount:N2}.",
                        ipAddress);

                    return await GetExpenseByIdAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating expense {Id}.", id);
                    response.Success = false;
                    response.Message = $"An error occurred: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            }

            // ==========================================================
            // SOFT DELETE
            // ==========================================================
            public async Task<ServiceResponse<bool>> SoftDeleteExpenseAsync(
                Guid id, string currentUserId, string ipAddress)
            {
                var response = new ServiceResponse<bool>();

                try
                {
                    var expense = await _context.Expenses
                        .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                    if (expense == null)
                    {
                        response.Success = false;
                        response.Message = "Expense not found.";
                        response.StatusCode = 404;
                        return response;
                    }

                    expense.IsDeleted = true;
                    expense.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    await _auditLog.LogActivityAsync(
                        currentUserId,
                        "Expense Deleted",
                        $"Ref {expense.ReferenceNumber}: ₦{expense.Amount:N2}.",
                        ipAddress);

                    response.Success = true;
                    response.Message = "Expense deleted.";
                    response.StatusCode = 200;
                    response.Data = true;
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting expense {Id}.", id);
                    response.Success = false;
                    response.Message = $"An error occurred: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            }

            // ==========================================================
            // DASHBOARD SUMMARY
            // ==========================================================
            public async Task<ServiceResponse<ExpenseDashboardDto>> GetExpenseDashboardAsync(
                Guid? academicSessionId, Guid? termId)
            {
                var response = new ServiceResponse<ExpenseDashboardDto>();

                try
                {
                    var query = _context.Expenses
                        .AsNoTracking()
                        .Where(e => !e.IsDeleted);

                    if (academicSessionId.HasValue)
                        query = query.Where(e => e.AcademicSessionId == academicSessionId.Value);

                    if (termId.HasValue)
                        query = query.Where(e => e.TermId == termId.Value);

                    var expenses = await query.ToListAsync();

                    var byCategory = expenses
                        .GroupBy(e => e.Category)
                        .Select(g => new ExpenseCategorySummaryDto
                        {
                            Category = g.Key,
                            Count = g.Count(),
                            TotalAmount = g.Sum(e => e.Amount),
                        })
                        .OrderByDescending(x => x.TotalAmount)
                        .ToList();

                    // 👈 new: breakdown by payment method
                    var byGateway = expenses
                        .GroupBy(e => e.PaymentGateway)
                        .Select(g => new ExpenseGatewaySummaryDto
                        {
                            Gateway = g.Key,
                            GatewayName = g.Key.ToString(),
                            Count = g.Count(),
                            TotalAmount = g.Sum(e => e.Amount),
                        })
                        .OrderByDescending(x => x.TotalAmount)
                        .ToList();

                    response.Success = true;
                    response.StatusCode = 200;
                    response.Data = new ExpenseDashboardDto
                    {
                        TotalExpenses = expenses.Sum(e => e.Amount),
                        TotalTransactions = expenses.Count,
                        ByCategory = byCategory,
                        ByGateway = byGateway,
                    };
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error building expense dashboard.");
                    response.Success = false;
                    response.Message = $"An error occurred: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            }

            // ==========================================================
            // HELPER — auto-generate reference
            // ==========================================================
            private async Task<string> GenerateReferenceNumberAsync()
            {
                var today = DateTime.UtcNow.ToString("yyyyMMdd");

                for (int attempt = 0; attempt < 5; attempt++)
                {
                    var suffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
                    var reference = $"EXP-{today}-{suffix}";

                    bool exists = await _context.Expenses
                        .AnyAsync(e => e.ReferenceNumber == reference);

                    if (!exists) return reference;
                }

                return $"EXP-{today}-{DateTime.UtcNow.Ticks.ToString()[^6..]}";
            }
        }
}
