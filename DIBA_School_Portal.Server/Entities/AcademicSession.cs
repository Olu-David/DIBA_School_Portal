using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class AcademicSession
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., "2026/2027"

        public bool IsCurrentSession { get; set; } = false;

        public ICollection<SchoolTerm> Terms { get; set; } = new List<SchoolTerm>();
        public ICollection<Fee> Fees { get; set; } = new List<Fee>();
        public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    }
}
