namespace DIBA_School_Portal.Server.Contracts
{
    public class StudentTermReportDto
    {
        // ---------- Identity ----------
        public Guid TermResultId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;

        // ── NEW ──
        public string? AdmissionNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // ---------- Context ----------
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public Guid AcademicSessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;

        // ---------- Performance ----------
        public decimal AverageScore { get; set; }
        public int ClassPosition { get; set; }
        public string OverallGrade { get; set; } = string.Empty;

        // ── NEW ──
        public string? TeacherRemark { get; set; }
        public string? PrincipalRemark { get; set; }

        public List<TermSubjectResultDto> SubjectResults { get; set; } = new();
    }

   
}