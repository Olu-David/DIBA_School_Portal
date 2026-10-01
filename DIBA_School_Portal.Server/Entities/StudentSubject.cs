using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class StudentSubject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public virtual StudentProfile? StudentProfile { get; set; }
        public Guid SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public virtual Subject? Subject { get; set; }
        public Guid AcademicSessionId { get; set; }
    }
}
