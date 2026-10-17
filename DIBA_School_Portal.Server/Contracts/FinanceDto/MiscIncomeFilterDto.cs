using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class MiscIncomeFilterDto
    {
        public Guid? AcademicSessionId { get; set; }
        public Guid? TermId { get; set; }
        public string? Category { get; set; }
        public PaymentGateway? Gateway { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
