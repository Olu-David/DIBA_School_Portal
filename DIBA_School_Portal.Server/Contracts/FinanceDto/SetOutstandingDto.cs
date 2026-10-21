namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class SetOutstandingDto
    {
        public Guid AcademicSessionId { get; set; }
        public Guid TermId { get; set; }
        public decimal Amount { get; set; }
    }
}
