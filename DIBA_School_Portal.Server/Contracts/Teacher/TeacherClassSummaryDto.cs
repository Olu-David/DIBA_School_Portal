using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.Teacher
{
    public class TeacherClassSummaryDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public AcademicSection Section { get; set; }
    }
}
