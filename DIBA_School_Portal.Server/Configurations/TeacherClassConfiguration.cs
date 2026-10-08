using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class TeacherClassConfiguration : IEntityTypeConfiguration<TeacherClass>
    {
        public void Configure(EntityTypeBuilder<TeacherClass> builder)
        {
            builder.HasKey(x => x.Id);

            // Prevent the same (Teacher, Class) pair twice
            builder.HasIndex(x => new { x.TeacherId, x.SchoolClassId })
                   .IsUnique();

            builder.HasOne(x => x.Teacher)
                   .WithMany(t => t.TeacherClasses)
                   .HasForeignKey(x => x.TeacherId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.SchoolClass)
                   .WithMany(c => c.TeacherClasses)
                   .HasForeignKey(x => x.SchoolClassId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Performance: filter by class
            builder.HasIndex(x => x.SchoolClassId);
        }
    }
}