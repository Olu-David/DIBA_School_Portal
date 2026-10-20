using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.PromotionDto
{
    public class StudentPreviewDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public ClassStream? CurrentStream { get; set; }
        public bool NeedsStream { get; set; }
    }
}
