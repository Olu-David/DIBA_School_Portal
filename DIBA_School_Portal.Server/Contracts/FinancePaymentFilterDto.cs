using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts
{
    public class FinancePaymentFilterDto
    {
        public Guid? AcademicSessionId { get; set; }
        public Guid? TermId { get; set; }
        public Guid? ClassId { get; set; }
        public PaymentGateway? Gateway { get; set; }
        public PaymentStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }   // matches student name / admission / reference
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
