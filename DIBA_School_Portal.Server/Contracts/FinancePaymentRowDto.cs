using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts
{
    public class FinancePaymentRowDto
    {
        public Guid Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public PaymentGateway Gateway { get; set; }
        public string GatewayName => Gateway.ToString();
        public PaymentStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime PaymentDate { get; set; }
        public string? RecordedBy { get; set; }
        public string? Note { get; set; }
    }
}
