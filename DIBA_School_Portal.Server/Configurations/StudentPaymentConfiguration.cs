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

            builder.HasIndex(x => x.ReferenceNumber)
                   .IsUnique();

            builder.HasIndex(x => new { x.StudentProfileId, x.TermId });

            // ---------- Student ----------
            builder.HasOne(x => x.StudentProfile)
                   .WithMany()
                   .HasForeignKey(x => x.StudentProfileId)
                   .OnDelete(DeleteBehavior.Restrict);       // ← NO ACTION

            // ---------- Session ----------
            builder.HasOne(x => x.AcademicSession)
                   .WithMany()
                   .HasForeignKey(x => x.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);       // ← NO ACTION

            // ---------- Term ----------
            builder.HasOne(x => x.Term)
                   .WithMany()
                   .HasForeignKey(x => x.TermId)
                   .OnDelete(DeleteBehavior.Restrict);       // ← NO ACTION

            // ---------- Enrollment (optional) ----------
            builder.HasOne(x => x.StudentEnrollment)
                   .WithMany()
                   .HasForeignKey(x => x.EnrollmentId)
                   .OnDelete(DeleteBehavior.Restrict)        // ← NO ACTION
                   .IsRequired(false);

            // ---------- Payment items ----------
            builder.HasMany(x => x.PaymentItems)
                   .WithOne(pi => pi.StudentPayment)
                   .HasForeignKey(pi => pi.StudentPaymentId)
                   .OnDelete(DeleteBehavior.Cascade);        // ← OK, cascade here is fine (child owns nothing else)
        }
    }
}