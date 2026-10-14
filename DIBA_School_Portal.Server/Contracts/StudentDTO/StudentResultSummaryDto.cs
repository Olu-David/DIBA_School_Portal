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
        public ClassStream? Stream { get; set; }
        public decimal AverageScore { get; set; }
        public decimal TotalMarksObtained { get; set; }
        public int Position { get; set; }
        public string OverallGrade { get; set; } = string.Empty;
        public int SubjectCount { get; set; }

        // ---- Progress ----
        public int SubmittedSubjectCount { get; set; }   // subjects with IsDraft = false
        public int TotalSubjectCount { get; set; }        // subjects the class teaches

        // Computed — no DB column. True when every subject has been submitted.
        public bool IsComplete =>
            TotalSubjectCount > 0 && SubmittedSubjectCount == TotalSubjectCount;
    }
}