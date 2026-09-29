using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
    {
        public void Configure(EntityTypeBuilder<Enrollment> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.TotalAmountPaid).HasColumnType("decimal(18,2)");
            builder.Property(e => e.OutstandingBalance).HasColumnType("decimal(18,2)");
            builder.Property(e => e.IsFullyPaid).IsRequired();
            builder.Property(e => e.IsActive).IsRequired();

            builder.HasOne(e => e.StudentProfile)
                   .WithMany(sp => sp.Enrollments)
                   .HasForeignKey(e => e.StudentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            //builder.HasOne(e => e.ClassRoom)
            //       .WithMany(cr => cr.Enrollments)
            //       .HasForeignKey(e => e.ClassRoomId)
            //       .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AcademicSession)
                   .WithMany()
                   .HasForeignKey(e => e.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Term)
                   .WithMany()
                   .HasForeignKey(e => e.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.PaymentTransactions)
                   .WithOne(pt => pt.Enrollment)
                   .HasForeignKey(pt => pt.EnrollmentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.TermResults)
                   .WithOne(tr => tr.Enrollment)
                   .HasForeignKey(tr => tr.EnrollmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}