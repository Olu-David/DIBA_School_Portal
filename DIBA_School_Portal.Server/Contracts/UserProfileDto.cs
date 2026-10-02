namespace DIBA_School_Portal.Server.Contracts
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AdmissionNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public bool MustChangePassword { get; set; }

        /// Populated only for parents
        public List<ParentChildDto>? Children { get; set; }
    }
}
