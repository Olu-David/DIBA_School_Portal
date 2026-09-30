namespace DIBA_School_Portal.Server.Contracts
{
    public class TermSubjectResultDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public decimal Test1Score { get; set; }
        public decimal Test2Score { get; set; }
        public decimal ExamScore { get; set; }
        public decimal TotalScore { get; set; }
        public int SubjectPosition { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }
}
