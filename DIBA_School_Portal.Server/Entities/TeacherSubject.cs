using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class TeacherSubject
    {
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            // ---------- Teacher ----------
            [Required]
            public Guid TeacherId { get; set; }

            [ForeignKey(nameof(TeacherId))]
            public Teacher Teacher { get; set; } = null!;

            // ---------- Subject ----------
            [Required]
            public Guid SubjectId { get; set; }

            [ForeignKey(nameof(SubjectId))]
            public Subject Subject { get; set; } = null!;

            // ---------- Metadata ----------
            /// <summary>
            /// Marks the teacher's main subject (e.g. head of department).
            /// Only one per teacher should be true.
            /// </summary>
            public bool IsPrimarySubject { get; set; } = false;

            public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }
        }
}
