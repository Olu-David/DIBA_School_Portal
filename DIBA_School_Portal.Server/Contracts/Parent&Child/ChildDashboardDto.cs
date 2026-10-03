using DIBA_School_Portal.Server.Contracts.StudentDTO;

namespace DIBA_School_Portal.Server.Contracts
{
    public class ChildDashboardDto
    {
        public ParentChildSummaryDto Child { get; set; } = null!;

        public Guid? CurrentSessionId { get; set; }
        public string? CurrentSessionName { get; set; }
        public Guid? CurrentTermId { get; set; }
        public string? CurrentTermName { get; set; }

        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public bool IsFullyPaid { get; set; }

        public decimal LatestAverageScore { get; set; }
        public int LatestClassPosition { get; set; }
        public string? LatestOverallGrade { get; set; }

        public List<SubjectScoreDto> LatestSubjects { get; set; } = new();
        public List<StudentPaymentDto> RecentPayments { get; set; } = new();
        public List<ChildEnrollmentDto> Enrollments { get; set; } = new();
    }

}
