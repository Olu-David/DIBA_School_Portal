using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class ScoreEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentProfileId { get; set; }

        [ForeignKey(nameof(StudentProfileId))]
        public StudentProfile StudentProfile { get; set; } = null!;

        [Required]
        public Guid SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;

        [Required]
        public Guid AssessmentId { get; set; }

        [ForeignKey(nameof(AssessmentId))]
        public Assessment Assessment { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; } // Marks obtained by the student
    }
}
