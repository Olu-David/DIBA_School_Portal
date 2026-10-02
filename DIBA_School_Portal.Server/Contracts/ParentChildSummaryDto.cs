namespace DIBA_School_Portal.Server.Contracts
{
    public class ParentChildSummaryDto
    {
        public Guid StudentProfileId { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimaryContact { get; set; }

        public decimal OutstandingBalance { get; set; }
        public bool IsFullyPaid { get; set; }
        public decimal LatestAverageScore { get; set; }
        public string? LatestTermName { get; set; }

    }
}
