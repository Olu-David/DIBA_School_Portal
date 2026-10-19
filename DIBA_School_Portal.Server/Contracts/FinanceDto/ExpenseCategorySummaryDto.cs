namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class ExpenseCategorySummaryDto
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
