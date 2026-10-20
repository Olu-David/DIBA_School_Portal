namespace DIBA_School_Portal.Server.Contracts.PromotionDto
{
    public class PromotionResultDto
    {

        public Guid BatchId { get; set; }
        public int PromotedCount { get; set; }
        public int GraduatedCount { get; set; }
        public int TotalCount { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
