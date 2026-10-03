using DIBA_School_Portal.Server.Contracts.ClassDto;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolResultResponseDto
    {
        public Guid AcademicSessionId { get; set; }
        public string AcademicSessionName { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public string TermName { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Summary
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalPublished { get; set; }
        public int TotalPending { get; set; }
        public decimal OverallAverage { get; set; }

        // Breakdown
        public List<ClassResultGroupDto> Classes { get; set; } = new();
    }
}
