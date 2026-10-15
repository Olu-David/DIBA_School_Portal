using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class EnrollmentConfiguration : IEntityTypeConfiguration<StudentEnrollments>
    {
        public void Configure(EntityTypeBuilder<StudentEnrollments> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TotalAmountPaid)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.OutstandingBalance)
                   .HasColumnType("decimal(18,2)");

            builder.HasOne(x => x.StudentProfile)
                   .WithMany(sp => sp.Enrollments)
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

            builder.HasOne(x => x.SchoolClass)
                   .WithMany()
                   .HasForeignKey(x => x.SchoolClassId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.StudentPayment)
                   .WithOne(sp => sp.StudentEnrollment)
                   .HasForeignKey(sp => sp.EnrollmentId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.TermResults)
                   .WithOne(tr => tr.Enrollment)
                   .HasForeignKey(tr => tr.EnrollmentId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => new { x.StudentProfileId, x.AcademicSessionId, x.TermId })
                   .IsUnique();
        }
    }
}