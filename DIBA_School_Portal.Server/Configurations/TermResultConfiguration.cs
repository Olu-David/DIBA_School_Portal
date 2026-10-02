using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class TermResultConfiguration : IEntityTypeConfiguration<TermResult>
    {
        public void Configure(EntityTypeBuilder<TermResult> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StudentId)
                   .IsRequired()
                   .HasMaxLength(450);

            builder.Property(x => x.TotalMarksObtained)
                   .HasColumnType("decimal(6,2)");

            builder.Property(x => x.AverageScore)
                   .HasColumnType("decimal(5,2)");

            builder.Property(x => x.OverallGrade)
                   .HasMaxLength(10);

            builder.Property(x => x.TeacherRemark)
                   .HasMaxLength(500);

            builder.Property(x => x.PrincipalRemark)
                   .HasMaxLength(500);

            builder.HasOne(x => x.Student)
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.SchoolClass)
                   .WithMany()
                   .HasForeignKey(x => x.ClassId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Term)
                   .WithMany()
                   .HasForeignKey(x => x.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AcademicSession)
                   .WithMany()
                   .HasForeignKey(x => x.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ✅ Changed SetNull → Restrict
            builder.HasOne(x => x.Enrollment)
                   .WithMany(e => e.TermResults)
                   .HasForeignKey(x => x.EnrollmentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.SubjectResults)
                   .WithOne(sr => sr.TermResult)
                   .HasForeignKey(sr => sr.TermResultId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.StudentId, x.TermId, x.AcademicSessionId })
                   .IsUnique();
        }
    }
}