using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public Guid SchoolClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public AcademicSection Section { get; set; }
        public string SectionName => Section.ToString();
        public ClassStream? Stream { get; set; }
        public string? StreamName => Stream?.ToString();

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Aggregates
        public decimal OutstandingBalance { get; set; }
        public bool IsFullyPaid { get; set; }
        public decimal LatestAverageScore { get; set; }
        public int LatestClassPosition { get; set; }
        public string? LatestTermName { get; set; }

        public List<StudentParentLinkDto> Parents { get; set; } = new();
    }
}
