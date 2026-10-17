using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class RecordMiscIncomeDto
    {
        public string Category { get; set; } = string.Empty;   // required
        public string Description { get; set; } = string.Empty; // required
        public decimal Amount { get; set; }

        public string? PayerName { get; set; }
        public string? PayerPhone { get; set; }

        public Guid? StudentProfileId { get; set; }   // optional

        public Guid AcademicSessionId { get; set; }
        public Guid TermId { get; set; }

        public PaymentGateway Gateway { get; set; } = PaymentGateway.Cash;
        public string? ExternalReference { get; set; }
        public string? Note { get; set; }
    }
}
