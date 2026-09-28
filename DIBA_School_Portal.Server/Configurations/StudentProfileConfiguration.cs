using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
    {
        public void Configure(EntityTypeBuilder<StudentProfile> builder)
        {
            builder.HasKey(sp => sp.Id);
            builder.Property(sp => sp.UserId).IsRequired().HasMaxLength(450);
            builder.Property(sp => sp.AdmissionNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(sp => sp.AdmissionNumber).IsUnique(); // Ensure unique admission numbers

            builder.HasMany(sp => sp.Enrollments)
                   .WithOne(e => e.StudentProfile)
                   .HasForeignKey(e => e.StudentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
