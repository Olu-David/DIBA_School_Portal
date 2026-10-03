namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class ClassFinanceSummaryDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public decimal Expected { get; set; }
        public decimal Collected { get; set; }
        public decimal Outstanding { get; set; }
        public decimal CollectionRate { get; set; }
        public int FullyPaidCount { get; set; }
        public int UnpaidCount { get; set; }

    }
}
