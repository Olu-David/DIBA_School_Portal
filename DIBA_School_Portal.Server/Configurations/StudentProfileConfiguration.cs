using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class StudentProfileConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                   .IsRequired()
                   .HasMaxLength(450);

            builder.Property(x => x.AdmissionNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.FirstName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.LastName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(x => x.AdmissionNumber)
                   .IsUnique();

            builder.HasOne(x => x.SchoolClass)
                   .WithMany()
                   .HasForeignKey(x => x.SchoolClassId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Enrollments)
                   .WithOne(e => e.StudentProfile)
                   .HasForeignKey(e => e.StudentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StudentPayments)
                   .WithOne(p => p.StudentProfile)
                   .HasForeignKey(p => p.StudentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);


        }
    }
}
