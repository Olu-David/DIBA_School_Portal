namespace DIBA_School_Portal.Server.Entities
{
    public class Enquiry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // "Admission" | "Contact"
        public string Type { get; set; } = "Admission";

        // Child details (Admission only)
        public string? ChildFirstName { get; set; }
        public string? ChildLastName { get; set; }
        public string? ChildDateOfBirth { get; set; }
        public string? ChildGender { get; set; }
        public string? Section { get; set; }
        public string? PreviousSchool { get; set; }
        public string? PreferredStartTerm { get; set; }

        // Parent / sender
        public string ParentName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;

        // Contact-form only
        public string? Subject { get; set; }

        public string Message { get; set; } = string.Empty;

        // Lifecycle
        public string Status { get; set; } = "New";     // New | Contacted | Enrolled | Lost
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ContactedAt { get; set; }
    }
}
