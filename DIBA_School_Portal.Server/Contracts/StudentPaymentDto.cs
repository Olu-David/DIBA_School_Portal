using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts
{
    public class StudentPaymentDto
    {
        public Guid Id { get; set; }
        public Guid StudentProfileId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public PaymentGateway Gateway { get; set; }
        public PaymentStatus Status { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string? ProofOfPaymentUrl { get; set; }

    }
}