namespace DIBA_School_Portal.Server.Contracts
{
    public class LinkParentToStudentDto
    {
        public Guid StudentProfileId { get; set; }
        public string ParentFirstName { get; set; } = string.Empty;
        public string ParentLastName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public string Relationship { get; set; } = "Guardian";
        public bool IsPrimaryContact { get; set; } = false;
        public IFormFile? ParentProfilePicture { get;  set; }
    }
}
