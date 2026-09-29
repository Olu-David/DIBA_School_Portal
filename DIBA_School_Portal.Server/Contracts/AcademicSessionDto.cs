using DIBA_School_Portal.Server.Entities;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class AcademicSessionDto
    {
        public Guid Id { get; set; } // Empty on create, populated on edit/response

        [Required(ErrorMessage = "Session name is required")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., "2026/2027"
        public string? UserId { get; set; }
        public bool IsCurrentSession { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Handles terms coming in from or going to the form
        public List<SchoolTermDto> Terms { get; set; } = new();

    }
}
