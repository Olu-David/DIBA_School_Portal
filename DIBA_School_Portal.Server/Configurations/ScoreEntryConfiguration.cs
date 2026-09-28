using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ScoreEntryConfiguration : IEntityTypeConfiguration<ScoreEntry>
    {
        public void Configure(EntityTypeBuilder<ScoreEntry> builder)
        {
            builder.HasKey(se => se.Id);
            builder.Property(se => se.Score).HasColumnType("decimal(5,2)").IsRequired();

            // Ensure a student only has one score entry per subject per assessment test
            builder.HasIndex(se => new { se.StudentProfileId, se.SubjectId, se.AssessmentId }).IsUnique();

            builder.HasOne(se => se.StudentProfile)
                   .WithMany(sp => sp.ScoreEntries)
                   .HasForeignKey(se => se.StudentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(se => se.Subject)
                   .WithMany(s => s.ScoreEntries)
                   .HasForeignKey(se => se.SubjectId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(se => se.Assessment)
                   .WithMany(a => a.ScoreEntries)
                   .HasForeignKey(se => se.AssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}