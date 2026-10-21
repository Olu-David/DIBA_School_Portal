using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class MiscIncomeResponseDto
    {
        public Guid Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? PayerName { get; set; }
        public Guid? StudentProfileId { get; set; }
        public string? StudentName { get; set; }
        public PaymentGateway Gateway { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime RecordedAt { get; set; }

    }
}
