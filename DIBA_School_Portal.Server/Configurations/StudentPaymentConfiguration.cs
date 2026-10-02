using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class StudentPaymentConfiguration : IEntityTypeConfiguration<StudentPayment>
    {
        public void Configure(EntityTypeBuilder<StudentPayment> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AmountPaid)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ReferenceNumber)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.ProofOfPaymentUrl)
                   .HasMaxLength(255);

            // Student relationship — deleting a student cascades to their payments
            builder.HasOne(x => x.StudentProfile)
                   .WithMany(sp => sp.StudentPayments)
                   .HasForeignKey(x => x.StudentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Academic session — cannot delete a session that has payments
            builder.HasOne(x => x.AcademicSession)
                   .WithMany()
                   .HasForeignKey(x => x.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Term — cannot delete a term that has payments
            builder.HasOne(x => x.Term)
                   .WithMany()
                   .HasForeignKey(x => x.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Fee — optional; if the fee is deleted, keep the payment with null fee
            builder.HasOne(x => x.Fee)
                   .WithMany()
                   .HasForeignKey(x => x.FeeId)
                   .OnDelete(DeleteBehavior.SetNull);

            // ✅ Enrollment — cannot be deleted while payments exist
            builder.HasOne(x => x.StudentEnrollment)
                   .WithMany(se => se.StudentPayment)
                   .HasForeignKey(x => x.EnrollmentID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Payment items — deleting a payment cascades to its items
            builder.HasMany(x => x.PaymentItems)
                   .WithOne(pi => pi.StudentPayment)
                   .HasForeignKey(pi => pi.StudentPaymentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}