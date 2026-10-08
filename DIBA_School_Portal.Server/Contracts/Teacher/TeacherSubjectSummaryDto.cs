namespace DIBA_School_Portal.Server.Contracts.Teacher
{
    public class TeacherSubjectSummaryDto
    {
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
    }
}
