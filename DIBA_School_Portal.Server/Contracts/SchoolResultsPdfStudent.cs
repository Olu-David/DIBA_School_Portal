namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolResultsPdfStudent
    {
        public int Position { get; set; }
        public string StudentName { get; set; } = "";
        public string AdmissionNumber { get; set; } = "";
        public decimal AverageScore { get; set; }
        public string OverallGrade { get; set; } = "";
        public int SubmittedSubjectCount { get; set; }
        public int TotalSubjectCount { get; set; }
        public bool IsComplete { get; set; }
    }
}
