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
        public string FeeName { get; set; } = string.Empty; // e.g., "Tuition Fee"

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumAmountToPay { get; set; }


        [Required]
        public Guid? ClassLevelId { get; set; }

        [ForeignKey(nameof(ClassLevelId))]
        public SchoolClass ClassLevel { get; set; } = null!;

        [Required]
        public Guid? AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        [Required]
        public Guid? TermId { get; set; }

        [ForeignKey(nameof(TermId))]
        public SchoolTerm Term { get; set; } = null!;
        public Guid? SchoolClassId { get; set; }

        [ForeignKey(nameof(SchoolClassId))]
        public virtual SchoolClass SchoolClass { get; set; } = null!;
       
        public AcademicSection Section { get; set; }
        public ClassStream ClassStream { get; set; }
        // Navigation property for the many-to-many link to classes
        public ICollection<ClassFeePrice> ClassFees { get; set; } = new List<ClassFeePrice>();
    }
}

