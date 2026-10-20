using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.PromotionDto
{
    public class ClassPromotionPreviewDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public AcademicSection AcademicSection { get; set; }

        public int StudentCount { get; set; }
        public Guid? DestinationClassId { get; set; }
        public string DestinationClassName { get; set; } = string.Empty;
        public bool IsGraduating { get; set; }
        public bool RequiresStreamSelection { get; set; }

        public List<StudentPreviewDto> Students { get; set; } = new();
    }
}
