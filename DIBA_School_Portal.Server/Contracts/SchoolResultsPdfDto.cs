namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolResultsPdfDto
    {
        public string AcademicSessionName { get; set; } = "";
        public string TermName { get; set; } = "";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalComplete { get; set; }
        public int TotalPending { get; set; }
        public decimal OverallAverage { get; set; }

        public List<SchoolResultsPdfClass> Classes { get; set; } = new();
    }
}
    