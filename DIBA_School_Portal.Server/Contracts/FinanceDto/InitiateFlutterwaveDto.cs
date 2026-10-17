namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class InitiateFlutterwaveDto
    {
        public Guid StudentProfileId { get; set; }
        public Guid AcademicSessionId { get; set; }
        public Guid TermId { get; set; }
        public decimal Amount { get; set; }

        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
    }
}
