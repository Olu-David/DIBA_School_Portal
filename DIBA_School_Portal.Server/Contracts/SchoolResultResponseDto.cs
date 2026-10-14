namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class SchoolResultResponseDto
    {
        public Guid AcademicSessionId { get; set; }
        public string AcademicSessionName { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public string TermName { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; }

        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }

    
        public int TotalComplete { get; set; }   // was TotalPublished
        public int TotalPending { get; set; }

        public decimal OverallAverage { get; set; }

        public List<ClassResultGroupDto> Classes { get; set; } = new();
    }
}