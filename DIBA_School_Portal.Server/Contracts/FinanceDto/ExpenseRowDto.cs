using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class ExpenseRowDto
    {

        public Guid Id { get; set; }
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public PaymentGateway PaymentGateway { get; set; }   
        public string GatewayName { get; set; } = "";    
        public string? Vendor { get; set; }
        public string ReferenceNumber { get; set; } = "";
        public DateTime ExpenseDate { get; set; }
        public string? ReceiptUrl { get; set; }
        public string? Notes { get; set; }
        public string? RecordedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
