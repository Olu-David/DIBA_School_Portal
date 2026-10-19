using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.ExpenseDtos
{
    public class ExpenseFilterDto
    {

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Category { get; set; }
        public PaymentGateway? PaymentGateway { get; set; }   // 👈 new filter
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public Guid? AcademicSessionId { get; set; }
        public Guid? TermId { get; set; }
    }
}
