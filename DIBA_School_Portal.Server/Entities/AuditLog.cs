namespace DIBA_School_Portal.Server.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }                          // Who performed the action
        public string? Action { get; set; }                          // What type of action (e.g., "Candidate Deleted")
        public string? Details { get; set; }                         // Specific description of what changed
        public string? IpAddress { get; set; }                       // The IP address the request came from
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;   // When it happened
    }
}