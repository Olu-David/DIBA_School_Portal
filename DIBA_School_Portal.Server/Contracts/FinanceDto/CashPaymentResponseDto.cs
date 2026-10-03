namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class CashPaymentResponseDto
    {
        public Guid PaymentId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal NewOutstandingBalance { get; set; }
        public bool IsFullyPaid { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
