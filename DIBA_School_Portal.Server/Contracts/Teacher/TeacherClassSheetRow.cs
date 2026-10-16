namespace DIBA_School_Portal.Server.Contracts.Teacher
{
    public class TeacherClassSheetRow
    {
        public int Position { get; set; }
        public string StudentName { get; set; } = "";
        public string AdmissionNumber { get; set; } = "";
        public decimal Test1Score { get; set; }
        public decimal Test2Score { get; set; }
        public decimal ExamScore { get; set; }
        public decimal TotalScore { get; set; }
        public string Grade { get; set; } = "";
        public string Remark { get; set; } = "";
    }
}
