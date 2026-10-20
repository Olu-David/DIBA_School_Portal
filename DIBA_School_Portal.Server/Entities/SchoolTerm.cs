using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class SchoolTerm
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., "First Term"

        public bool IsCurrentTerm { get; set; } = false;

        [Required]
        public Guid AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        public virtual ICollection<Fee> Fees { get; set; } = new List<Fee>();
        public int TermOrder { get; set; } // 1, 2, 3
        public DateTime? StartDate { get;  set; }
        public DateTime? EndDate { get;  set; }
    }
}
