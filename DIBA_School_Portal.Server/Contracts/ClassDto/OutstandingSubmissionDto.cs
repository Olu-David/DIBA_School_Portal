namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class OutstandingSubmissionDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public Guid? SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = "—";
        public int StudentCount { get; set; }
        public int ScoredCount { get; set; }
    }
}
