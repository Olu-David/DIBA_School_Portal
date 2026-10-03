namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    
       public class StudentReportCardDto
    {
        public Guid TermResultId { get; set; }
        public string? StudentId { get; set; }
        public string? StudentName { get; set; }
        public Guid ClassId { get; set; }
        public string? ClassName { get; set; }
        public Guid TermId { get; set; }
        public string? TermName { get; set; }
        public Guid AcademicSessionId { get; set; }
        public string? SessionName { get; set; }
        public decimal AverageScore { get; set; }
        public int ClassPosition { get; set; }
        public string? OverallGrade { get; set; }
        public List<SubjectScoreDto> SubjectResults { get; set; } = new();
    }
}   