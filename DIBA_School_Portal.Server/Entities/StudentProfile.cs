using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class StudentProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty; // ASP.NET Identity User ID

        [Required]
        [MaxLength(50)]
        public string AdmissionNumber { get; set; } = string.Empty; // e.g., "DIBA/2026/001"

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
        public ICollection<ScoreEntry> ScoreEntries { get; set; } = new List<ScoreEntry>();
    }
}
