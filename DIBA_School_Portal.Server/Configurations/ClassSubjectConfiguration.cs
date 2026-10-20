using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ClassSubjectConfiguration : IEntityTypeConfiguration<ClassSubject>
    {
        public void Configure(EntityTypeBuilder<ClassSubject> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(cs => cs.SchoolClass)
                   .WithMany(c => c.ClassSubjects)
                   .HasForeignKey(cs => cs.SchoolClassId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cs => cs.Subject)
                   .WithMany()
                   .HasForeignKey(cs => cs.SubjectId)
                   .OnDelete(DeleteBehavior.Restrict);   // see note below

            // PostgreSQL filtered unique index — double quotes, not brackets
            builder.HasIndex(cs => new { cs.SchoolClassId, cs.SubjectId })
                   .IsUnique()
                   .HasFilter("\"SubjectId\" IS NOT NULL")
                   .HasDatabaseName("UX_ClassSubjects_SchoolClassId_SubjectId");
        }
    }
}