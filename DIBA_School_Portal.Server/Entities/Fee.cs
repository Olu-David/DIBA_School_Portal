using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class Fee
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string FeeName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public Guid AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        [Required]
        public Guid TermId { get; set; }

        [ForeignKey(nameof(TermId))]
        public SchoolTerm Term { get; set; } = null!;

        // A Fee is a catalogue entry — the classes it applies to live in ClassFeePrices.
        // These are optional metadata for reporting/filtering.

        public Guid? SchoolClassId { get; set; }

        [ForeignKey(nameof(SchoolClassId))]
        public virtual SchoolClass? SchoolClass { get; set; }

        public AcademicSection? Section { get; set; }
        public ClassStream? ClassStream { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<ClassFeePrice> ClassFeePrices { get; set; } = new List<ClassFeePrice>();
    }
}