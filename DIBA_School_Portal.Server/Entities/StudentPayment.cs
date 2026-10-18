using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    /// <summary>
    /// One payment transaction. Money that entered the school's account
    /// in a single event — via Flutterwave, Paystack, cash, or bank transfer.
    /// A single payment can be split across multiple fees — see PaymentItems.
    /// </summary>
    public class StudentPayment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // ---------- Who paid ----------
        [Required]
        public Guid StudentProfileId { get; set; }

        [ForeignKey(nameof(StudentProfileId))]
        public Student StudentProfile { get; set; } = null!;

        // ---------- Which term ----------
        [Required]
        public Guid AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        [Required]
        public Guid TermId { get; set; }

        [ForeignKey(nameof(TermId))]
        public SchoolTerm Term { get; set; } = null!;

        // ---------- Enrollment link (nullable — payments can precede enrollment) ----------
        public Guid? EnrollmentId { get; set; }

        [ForeignKey(nameof(EnrollmentId))]
        public StudentEnrollment? StudentEnrollment { get; set; }

        // ---------- The transaction ----------
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required]
        public PaymentGateway Gateway { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // ---------- Optional ----------
        [MaxLength(255)]
        public string? ProofOfPaymentUrl { get; set; }

        // ---------- Line items ----------
        public ICollection<StudentPaymentItem> PaymentItems { get; set; }
            = new List<StudentPaymentItem>();
    }
}