namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentParentLinkDto
    {
        public string ParentUserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimaryContact { get; set; }
    }
}

