using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.PromotionDto
{
    public class PromotionRecordDto
    {
        public Guid Id { get; set; }
        public Guid BatchId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FromClassName { get; set; } = string.Empty;
        public string? ToClassName { get; set; }
        public ClassStream? Stream { get; set; }
        public DateTime PromotedAt { get; set; }
    }
}
