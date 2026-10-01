using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class StudentEnrollments
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentProfileId { get; set; }

        [ForeignKey(nameof(StudentProfileId))]
        public StudentProfile StudentProfile { get; set; } = null!;

        [Required]
        public Guid AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        [Required]
        public Guid TermId { get; set; }

        [ForeignKey(nameof(TermId))]
        public SchoolTerm Term { get; set; } = null!;

        public Guid SchoolClassId { get; set; }
        public SchoolClass SchoolClass { get; set; } = null!;    

        // --- Direct Financial Tracking for this Term ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountPaid { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingBalance { get; set; } = 0.00m;

        public bool IsFullyPaid { get; set; } = false;
         public StudentEnrollmentStatus Status { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;

        public ICollection<StudentPayment> StudentPayment { get; set; } = new List<StudentPayment>();
        public ICollection<TermResult> TermResults { get; set; } = new List<TermResult>();
    }
}
