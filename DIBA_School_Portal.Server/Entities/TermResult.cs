using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class TermResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid EnrollmentId { get; set; }

        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment Enrollment { get; set; } = null!;

        [Column(TypeName = "decimal(6,2)")]
        public decimal TotalMarksObtained { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal AverageScore { get; set; }

        public int ClassPosition { get; set; }

        [MaxLength(500)]
        public string TeacherRemark { get; set; } = string.Empty;

        [MaxLength(500)]
        public string PrincipalRemark { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = false;
    }
}
