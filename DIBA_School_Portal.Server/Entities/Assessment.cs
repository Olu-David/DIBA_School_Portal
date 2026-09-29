using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class Assessment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Test 1", "Midterm", "Exam"

        [Required]
        [Range(0, 100)]
        public decimal WeightPercentage { get; set; } // e.g., 10.0 for 10%

        [Required]
        public Guid ClassLevelId { get; set; }

        [ForeignKey(nameof(ClassLevelId))]
        public SchoolClass ClassLevel { get; set; } = null!;

        [Required]
        public Guid AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        [Required]
        public Guid TermId { get; set; }

        [ForeignKey(nameof(TermId))]
        public SchoolTerm Term { get; set; } = null!;

        public ICollection<ScoreEntry> ScoreEntries { get; set; } = new List<ScoreEntry>();
    }
}

