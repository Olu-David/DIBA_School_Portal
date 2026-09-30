namespace DIBA_School_Portal.Server.Contracts
{
    public class StudentTermReportDto
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
        public decimal PreviousTermAverage { get; set; }
        public decimal PerformanceChangePercentage { get; set; } // e.g. +5.4% or -2.1%
        public string? PerformanceTrend { get; set; } // "Increased", "Dropped", "Stable", or "First Term"

        public int? ClassPosition { get; set; }
        public string? OverallGrade { get; set; }
        public List<TermSubjectResultDto> SubjectResults { get; set; } = new();
      
    }
}