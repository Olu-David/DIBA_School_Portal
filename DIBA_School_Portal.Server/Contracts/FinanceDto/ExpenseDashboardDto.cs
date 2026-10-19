namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class ExpenseDashboardDto
    {
        public decimal TotalExpenses { get; set; }
        public int TotalTransactions { get; set; }
        public List<ExpenseCategorySummaryDto> ByCategory { get; set; } = new();
        public List<ExpenseGatewaySummaryDto> ByGateway { get; set; } = new();

    }
}
