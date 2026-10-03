using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class CreatePaymentDto
    {
        public Guid StudentProfileId { get; set; }
        public decimal AmountPaid { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public PaymentGateway Gateway { get; set; }
        public Guid AcademicSessionId { get; set; }
        public Guid TermId { get; set; }
        public Guid? FeeId { get; set; }
        public string? ProofOfPaymentUrl { get; set; }
    }
}