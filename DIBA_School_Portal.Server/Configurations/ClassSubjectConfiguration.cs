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

                builder.HasIndex(cs => new { cs.SchoolClassId, cs.SubjectId })
                       .IsUnique();

                builder.HasOne(cs => cs.SchoolClass)
                       .WithMany(sc => sc.ClassSubjects)
                       .HasForeignKey(cs => cs.SchoolClassId)
                       .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(cs => cs.Subject)
                       .WithMany(s => s.ClassSubjects)
                       .HasForeignKey(cs => cs.SubjectId)
                       .OnDelete(DeleteBehavior.Cascade);
            }
        }

    }
