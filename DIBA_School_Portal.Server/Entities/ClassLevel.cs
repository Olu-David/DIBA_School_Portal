using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class ClassLevel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., "JSS 1", "SS 1"

        public int DisplayOrder { get; set; }

        public ICollection<ClassRoom> ClassRooms { get; set; } = new List<ClassRoom>();
        public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
        public ICollection<Fee> Fees { get; set; } = new List<Fee>();
        public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    }
}
