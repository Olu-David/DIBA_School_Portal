using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
    {
        public void Configure(EntityTypeBuilder<TeacherSubject> builder)
        {
            builder.HasKey(x => x.Id);

            // Prevent the same (Teacher, Subject) pair twice
            builder.HasIndex(x => new { x.TeacherId, x.SubjectId })
                   .IsUnique();

            builder.HasOne(x => x.Teacher)
                   .WithMany(t => t.TeacherSubjects)
                   .HasForeignKey(x => x.TeacherId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Subject)
                   .WithMany(s => s.TeacherSubjects)
                   .HasForeignKey(x => x.SubjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Performance: filter by teacher
            builder.HasIndex(x => x.TeacherId);
        }
    }
}