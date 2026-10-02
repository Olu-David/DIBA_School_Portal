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

            builder.HasOne(x => x.StudentProfile)
                   .WithMany(sp => sp.StudentPayments)
                   .HasForeignKey(x => x.StudentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.AcademicSession)
                   .WithMany()
                   .HasForeignKey(x => x.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Term)
                   .WithMany()
                   .HasForeignKey(x => x.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Fees)
                   .WithMany()
                   .HasForeignKey(x => x.FeeId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.StudentEnrollment)
                   .WithMany(se => se.StudentPayment)
                   .HasForeignKey(x => x.EnrollmentID)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.PaymentItems)
                   .WithOne(pi => pi.StudentPayment)
                   .HasForeignKey(pi => pi.StudentPaymentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
