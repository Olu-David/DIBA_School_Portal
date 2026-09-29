using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolTermDto
    {
        public Guid Id { get; set; } // Empty on create, populated on edit/response

        [Required(ErrorMessage = "Term name is required")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., "First Term"

        public bool IsCurrentTerm { get; set; }
        public Guid AcademicSessionId { get; set; }
        public DateTime? StartDate { get;  set; }
        public DateTime? EndDate { get;  set; }
    }
}
