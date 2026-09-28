using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class PaymentTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentProfileId { get; set; }

        [ForeignKey(nameof(StudentProfileId))]
        public StudentProfile StudentProfile { get; set; } = null!;

        [Required]
        public Guid EnrollmentId { get; set; }

        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment Enrollment { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty; // Gateway reference or Teller #

        [Required]
        public PaymentGateway Gateway { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? ProofOfPaymentUrl { get; set; } // Supabase link for manual bank teller uploads
    }
}
