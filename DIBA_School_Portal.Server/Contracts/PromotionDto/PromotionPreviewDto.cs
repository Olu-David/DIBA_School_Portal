namespace DIBA_School_Portal.Server.Contracts.PromotionDto
{
    public class PromotionPreviewDto
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime? ThirdTermEndDate { get; set; }
        public bool ThirdTermEnded { get; set; }
        public bool IsReady { get; set; }

        public int TotalStudents { get; set; }
        public int TotalGraduating { get; set; }
        public int TotalRequiresStream { get; set; }

        public List<ClassPromotionPreviewDto> Classes { get; set; } = new();
    }
}
