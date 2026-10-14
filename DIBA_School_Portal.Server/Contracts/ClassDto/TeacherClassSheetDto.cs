namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class TeacherClassSheetDto
    {
        public string ClassName { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public string TermName { get; set; } = "";
        public string AcademicSessionName { get; set; } = "";
        public bool IsSubmitted { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public decimal ClassAverage { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }

        public List<TeacherClassSheetRow> Students { get; set; } = new();
    }
}
