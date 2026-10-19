using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class StudentSubject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }        // 👈 renamed from StudentProfile to Student

        public Guid? SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual Subject? Subject { get; set; }

        public Guid AcademicSessionId { get; set; }
    }
}