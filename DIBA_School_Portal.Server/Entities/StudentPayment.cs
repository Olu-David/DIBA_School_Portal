using DIBA_School_Portal.Server.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class StudentPayment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentProfileId { get; set; }

        [ForeignKey(nameof(StudentProfileId))]
        public Student StudentProfile { get; set; } = null!;

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
        public Guid AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        [Required]
        public Guid TermId { get; set; }

        [ForeignKey(nameof(TermId))]
        public SchoolTerm Term { get; set; } = null!;

        public Guid? FeeId { get; set; }

        [ForeignKey(nameof(FeeId))]
        public virtual Fee? Fee { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? ProofOfPaymentUrl { get; set; }

       
        [Required]
        public Guid EnrollmentID { get; set; }

        [ForeignKey(nameof(EnrollmentID))]
        public virtual StudentEnrollments StudentEnrollment { get; set; } = null!;

        public ICollection<StudentPaymentItem> PaymentItems { get; set; } = new List<StudentPaymentItem>();
    }
}