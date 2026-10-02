using DIBA_School_Portal.Server.Configurations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class ParentStudent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string ParentUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ParentUserId))]
        public ApplicationUser Parent { get; set; } = null!;

        [Required]
        public Guid StudentProfileId { get; set; }

        [ForeignKey(nameof(StudentProfileId))]
        public Student StudentProfile { get; set; } = null!;

        /// "Father", "Mother", "Guardian", "Uncle", "Aunt", etc.
        [MaxLength(50)]
        public string Relationship { get; set; } = "Guardian";

        /// Primary contact for urgent matters (SMS/calls)
        public bool IsPrimaryContact { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
