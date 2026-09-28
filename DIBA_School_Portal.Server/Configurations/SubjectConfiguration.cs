using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Code).IsRequired().HasMaxLength(20);
            builder.HasIndex(s => s.Code).IsUnique();

            builder.HasMany(s => s.ClassSubjects)
                   .WithOne(cs => cs.Subject)
                   .HasForeignKey(cs => cs.SubjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.ScoreEntries)
                   .WithOne(se => se.Subject)
                   .HasForeignKey(se => se.SubjectId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}