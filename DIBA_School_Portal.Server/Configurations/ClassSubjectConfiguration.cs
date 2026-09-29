using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ClassSubjectConfiguration : IEntityTypeConfiguration<ClassSubject>
    {
        public void Configure(EntityTypeBuilder<ClassSubject> builder)
        {
            builder.HasKey(cs => cs.Id);

            // Prevent duplicate subject assignment to the same class level
            builder.HasIndex(cs => new { cs.SchoolClassId, cs.SubjectId }).IsUnique();
        }
    }
}