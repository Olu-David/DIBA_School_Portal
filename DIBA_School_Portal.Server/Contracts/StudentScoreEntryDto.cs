using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class StudentScoreEntryDto
    {
        public string StudentId { get; set; } = string.Empty; // Uses Student User ID
        public decimal Test1Score { get; set; }
        public decimal Test2Score { get; set; }
        public decimal ExamScore { get; set; }
        public string? StudentName { get; set; }
    }
}
