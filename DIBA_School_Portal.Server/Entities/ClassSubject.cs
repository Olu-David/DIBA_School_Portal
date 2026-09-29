using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class ClassSubject
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SchoolClassId { get; set; }

        [ForeignKey(nameof(SchoolClassId))]
        public SchoolClass SchoolClass { get; set; } = null!;

        [Required]
        public Guid SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;
    }
}
