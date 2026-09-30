namespace DIBA_School_Portal.Server.Contracts
{
    public class TermResultDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public decimal TotalMarksObtained { get; set; }
        public decimal AverageScore { get; set; }
        public int ClassPosition { get; set; }
        public string TeacherRemark { get; set; } = string.Empty;
        public string PrincipalRemark { get; set; } = string.Empty;
        public bool IsPublished { get; set; }

        // The breakdown of all subjects taken this term
        public List<TermSubjectResultDto> SubjectResults { get; set; } = new();
    }
}
