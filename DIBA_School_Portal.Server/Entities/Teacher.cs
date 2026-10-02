using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class Teacher
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string StaffId { get; set; } = string.Empty; // e.g., "DIBA/STF/042"

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Qualification { get; set; } = string.Empty;

        public DateTime DateEmployed { get; set; } = DateTime.UtcNow;
    }
}
