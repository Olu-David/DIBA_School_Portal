using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ExpenseDtos;
using DIBA_School_Portal.Server.Contracts.FinanceDto;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IExpenseService
    {
        Task<ServiceResponse<PaginatedViewModel<ExpenseRowDto>>> GetExpensesAsync(
           ExpenseFilterDto filter);

        Task<ServiceResponse<ExpenseRowDto>> GetExpenseByIdAsync(Guid id);

        Task<ServiceResponse<ExpenseRowDto>> CreateExpenseAsync(
            CreateExpenseDto request, string currentUserId, string ipAddress);

        Task<ServiceResponse<ExpenseRowDto>> UpdateExpenseAsync(
            Guid id, UpdateExpenseDto request, string currentUserId, string ipAddress);

        Task<ServiceResponse<bool>> SoftDeleteExpenseAsync(
            Guid id, string currentUserId, string ipAddress);

        Task<ServiceResponse<ExpenseDashboardDto>> GetExpenseDashboardAsync(
            Guid? academicSessionId, Guid? termId);
    }
}
