
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class Subject
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Mathematics"

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty; // e.g., "MTH101"

        public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
      
    }
}
