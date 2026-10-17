using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Entities
{
  
    public class MiscellaneousIncome
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Free-form category. Deliberately a string, not an enum,
        /// so schools can add new categories without a migration.
        /// Suggested values: "Uniform", "Textbook", "ID Card", "Fine",
        /// "Donation", "PTA", "Excursion", "Exam Fee", "Other".
        /// </summary>
        public string Category { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        // ---------- Optional payer identity ----------
        public string? PayerName { get; set; }
        public string? PayerPhone { get; set; }

        // ---------- Optional link to a student ----------
        // Set this only if the payment relates to a specific student
        // (e.g. a lost-textbook fine). Leave null for walk-in customers,
        // donors, external exam candidates, etc.
        public Guid? StudentProfileId { get; set; }
        public Student? StudentProfile { get; set; }

        // ---------- Accounting context ----------
        public Guid AcademicSessionId { get; set; }
        public AcademicSession AcademicSession { get; set; } = null!;

        public Guid TermId { get; set; }
        public SchoolTerm Term { get; set; } = null!;

        public PaymentGateway Gateway { get; set; } = PaymentGateway.Cash;
        public PaymentStatus Status { get; set; } = PaymentStatus.Successful;

        // ---------- Audit ----------
        public string? RecordedByUserId { get; set; }
        public ApplicationUser? RecordedBy { get; set; }

        public string? Note { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}