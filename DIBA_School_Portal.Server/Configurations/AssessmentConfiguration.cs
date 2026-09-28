using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
    {
        public void Configure(EntityTypeBuilder<Assessment> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
            builder.Property(a => a.WeightPercentage).HasColumnType("decimal(5,2)").IsRequired();

            builder.HasOne(a => a.ClassLevel)
                   .WithMany(cl => cl.Assessments)
                   .HasForeignKey(a => a.ClassLevelId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.AcademicSession)
                   .WithMany(s => s.Assessments)
                   .HasForeignKey(a => a.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Term)
                   .WithMany(t => t.Assessments)
                   .HasForeignKey(a => a.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.ScoreEntries)
                   .WithOne(se => se.Assessment)
                   .HasForeignKey(se => se.AssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}