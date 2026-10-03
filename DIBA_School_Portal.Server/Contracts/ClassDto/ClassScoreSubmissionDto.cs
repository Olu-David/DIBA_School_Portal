using DIBA_School_Portal.Server.Contracts.StudentDTO;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class ClassScoreSubmissionDto
    {
        public Guid SubjectId { get; set; }
        public Guid ClassId { get; set; } // Identifies the class (e.g., JSS 1A)
        public Guid TermId { get; set; }
        public Guid AcademicSessionId { get; set; }
        public List<StudentScoreEntryDto> StudentScores { get; set; } = new();
        public bool IsFinished { get;  set; }
    }
}
