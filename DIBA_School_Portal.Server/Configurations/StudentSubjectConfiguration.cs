using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class StudentSubjectConfiguration : IEntityTypeConfiguration<StudentSubject>
    {
        public void Configure(EntityTypeBuilder<StudentSubject> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Student)
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Subject)
                       .WithMany()
                       .HasForeignKey(x => x.SubjectId)
                       .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.StudentId, x.SubjectId, x.AcademicSessionId })
                       .IsUnique();
        }
    }
}
