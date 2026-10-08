using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class TeacherAssignmentDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public AcademicSection Section { get; set; }

        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;

        public int StudentCount { get; set; }
        public int ScoredCount { get; set; }
        public string SubmissionStatus { get; set; } = "NotStarted";
    }
}