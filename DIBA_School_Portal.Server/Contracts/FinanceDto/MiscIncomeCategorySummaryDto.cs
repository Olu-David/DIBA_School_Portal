namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class MiscIncomeCategorySummaryDto
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
