namespace DIBA_School_Portal.Server.Contracts.Teacher
{
    public class TeacherDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        public string StaffId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public string Qualification { get; set; } = string.Empty;
        public DateTime DateEmployed { get; set; }
        public string PrimaryRole { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Summary — subjects and classes they've taught
        public int SubjectCount { get; set; }
        public int ClassCount { get; set; }

        public List<TeacherSubjectSummaryDto> Subjects { get; set; } = new();
        public List<TeacherClassSummaryDto> Classes { get; set; } = new();

    }
}
