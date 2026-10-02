using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class TermSubjectResultConfiguration : IEntityTypeConfiguration<TermSubjectResult>
    {
        public void Configure(EntityTypeBuilder<TermSubjectResult> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Test1Score)
                   .HasColumnType("decimal(5,2)");

            builder.Property(x => x.Test2Score)
                   .HasColumnType("decimal(5,2)");

            builder.Property(x => x.ExamScore)
                   .HasColumnType("decimal(5,2)");

            builder.Property(x => x.TotalScore)
                   .HasColumnType("decimal(5,2)");

            builder.Property(x => x.Grade)
                   .HasMaxLength(5);

            builder.Property(x => x.Remark)
                   .HasMaxLength(100);

            builder.Property(x => x.TeacherId)
                   .IsRequired()
                   .HasMaxLength(450);

            // ⚠️ Subject relationship — restrict deletion if results exist
            builder.HasOne(x => x.Subject)
                   .WithMany()
                   .HasForeignKey(x => x.SubjectId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ⚠️ Teacher relationship — restrict deletion if they've recorded grades
            builder.HasOne(x => x.Teacher)
                   .WithMany()
                   .HasForeignKey(x => x.TeacherId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ⚠️ Do NOT configure the TermResult relationship here.
            // It's already configured in TermResultConfiguration:
            //     builder.HasMany(x => x.SubjectResults)
            //            .WithOne(sr => sr.TermResult)
            //            .HasForeignKey(sr => sr.TermResultId)
            //            .OnDelete(DeleteBehavior.Cascade);
            //
            // Configuring the same relationship from both sides causes EF Core conflicts.

            // One subject result per term result
            builder.HasIndex(x => new { x.TermResultId, x.SubjectId })
                   .IsUnique();
        }
    }
}