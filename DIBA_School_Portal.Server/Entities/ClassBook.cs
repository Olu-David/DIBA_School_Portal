using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class ClassBook
    {
      
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            [Required]
            [MaxLength(150)]
            public string Title { get; set; } = string.Empty; // e.g., "Mathematics Textbook Vol. 2"

            [MaxLength(300)]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Column(TypeName = "decimal(18,2)")]
            public decimal Price { get; set; }

            [Required]
            public Guid SchoolClassId { get; set; }
            [ForeignKey(nameof(SchoolClassId))]
            public SchoolClass SchoolClass { get; set; } = null!;

            [Required]
            public Guid AcademicSessionId { get; set; }
            [ForeignKey(nameof(AcademicSessionId))]
            public AcademicSession AcademicSession { get; set; } = null!;

            [Required]
            public Guid TermId { get; set; }
            [ForeignKey(nameof(TermId))]
            public SchoolTerm Term { get; set; } = null!;
        }
  }

