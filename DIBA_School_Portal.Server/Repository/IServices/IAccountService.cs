using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;


namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IAccountService
    {
        /// <summary>
        /// Aggregated finance overview for a session/term:
        /// expected vs collected, online vs cash, per class, per gateway.
        /// </summary>
        Task<ServiceResponse<FinanceDashboardDto>> GetDashboardAsync(
            Guid academicSessionId, Guid termId);

        /// <summary>
        /// Paginated list of every payment (any gateway) with filters.
        /// </summary>
        Task<ServiceResponse<PaginatedViewModel<FinancePaymentRowDto>>> GetPaymentsAsync(
            FinancePaymentFilterDto filter);

        /// <summary>
        /// Record a cash / offline payment made at the bursary.
        /// Sets gateway = Cash, status = Successful, updates enrollment totals.
        /// </summary>
        Task<ServiceResponse<CashPaymentResponseDto>> RecordCashPaymentAsync(
            RecordCashPaymentDto request, string currentUserId, string ipAddress);

        /// <summary>
        /// All students with outstanding balances for a term.
        /// </summary>
        Task<ServiceResponse<List<ArrearsRowDto>>> GetArrearsAsync(
            Guid academicSessionId, Guid termId, Guid? classId = null); Task<ServiceResponse<MiscIncomeResponseDto>> RecordMiscIncomeAsync(
    RecordMiscIncomeDto request, string currentUserId, string ipAddress);

        Task<ServiceResponse<PaginatedViewModel<MiscIncomeRowDto>>> GetMiscIncomeAsync(
            MiscIncomeFilterDto filter);


    }
}