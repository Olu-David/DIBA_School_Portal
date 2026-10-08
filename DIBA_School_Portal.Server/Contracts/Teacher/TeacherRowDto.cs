namespace DIBA_School_Portal.Server.Contracts.Teacher
{
    public class TeacherRowDto
    {
        public Guid Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Qualification { get; set; } = string.Empty;
        public string PrimaryRole { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime DateEmployed { get; set; }
    }
}
