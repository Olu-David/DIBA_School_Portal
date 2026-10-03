using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentResultSummaryDto
    {
        public Guid TermResultId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

        public decimal AverageScore { get; set; }
        public decimal TotalMarksObtained { get; set; }
        public int Position { get; set; }
        public string OverallGrade { get; set; } = string.Empty;
        public int SubjectCount { get; set; }
        public bool IsPublished { get; set; }
        public ClassStream? Stream { get;  set; }
    }
}
