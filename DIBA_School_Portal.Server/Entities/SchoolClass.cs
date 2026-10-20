using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class SchoolClass
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., "JSS 1", "SS 1"

        public int DisplayOrder { get; set; }

        public AcademicSection AcademicSection { get; set; }

        public Guid? NextClassId { get; set; }
        public virtual SchoolClass? NextClass { get; set; }
        public virtual ICollection<SchoolClass> PreviousClasses { get; set; } = new List<SchoolClass>();

        public virtual ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
        public virtual ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();
        public virtual ICollection<Fee> Fees { get; set; } = new List<Fee>();

        public bool IsDeleted { get; set; }
    }
}