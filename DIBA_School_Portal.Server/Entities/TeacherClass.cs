using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    /// <summary>
    /// Records which classes a teacher is assigned to teach in.
    /// Many-to-many between Teacher and SchoolClass.
    /// </summary>
    public class TeacherClass
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // ---------- Teacher ----------
        [Required]
        public Guid TeacherId { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public Teacher Teacher { get; set; } = null!;

        // ---------- Class ----------
        [Required]
        public Guid SchoolClassId { get; set; }

        [ForeignKey(nameof(SchoolClassId))]
        public SchoolClass SchoolClass { get; set; } = null!;

        // ---------- Metadata ----------
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}