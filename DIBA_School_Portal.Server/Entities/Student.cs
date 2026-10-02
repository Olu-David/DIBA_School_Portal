using DIBA_School_Portal.Server.Configurations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class Student
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty; // ASP.NET Identity User ID
        [ForeignKey(nameof( UserId))]
        public virtual ApplicationUser User { get; set; } = null!; 
        [Required]
        [MaxLength(50)]
        public string AdmissionNumber { get; set; } = string.Empty; // e.g., "DIBA/2026/001"

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // --- Added: Current Class Link ---
        [Required]
        public Guid SchoolClassId { get; set; }

        [ForeignKey(nameof(SchoolClassId))]
        public SchoolClass SchoolClass { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Updated Collections to match your actual entities ---
        public ICollection<StudentEnrollments> Enrollments { get; set; } = new List<StudentEnrollments>();

        // Aligns with your StudentPayment entity
        public ICollection<StudentPayment> StudentPayments { get; set; } = new List<StudentPayment>();
    }
}