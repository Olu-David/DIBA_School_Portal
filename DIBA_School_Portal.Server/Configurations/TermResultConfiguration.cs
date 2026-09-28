using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class TermResultConfiguration : IEntityTypeConfiguration<TermResult>
    {
        public void Configure(EntityTypeBuilder<TermResult> builder)
        {
            builder.HasKey(tr => tr.Id);
            builder.Property(tr => tr.TotalMarksObtained).HasColumnType("decimal(6,2)");
            builder.Property(tr => tr.AverageScore).HasColumnType("decimal(5,2)");
            builder.Property(tr => tr.TeacherRemark).HasMaxLength(500);
            builder.Property(tr => tr.PrincipalRemark).HasMaxLength(500);
            builder.Property(tr => tr.IsPublished).IsRequired();

            builder.HasOne(tr => tr.Enrollment)
                   .WithMany(e => e.TermResults)
                   .HasForeignKey(tr => tr.EnrollmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}