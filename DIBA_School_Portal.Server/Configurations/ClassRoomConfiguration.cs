using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ClassRoomConfiguration : IEntityTypeConfiguration<ClassRoom>
    {
        public void Configure(EntityTypeBuilder<ClassRoom> builder)
        {
            builder.HasKey(cr => cr.Id);
            builder.Property(cr => cr.Name).IsRequired().HasMaxLength(20);

            builder.HasMany(cr => cr.Enrollments)
                   .WithOne(e => e.ClassRoom)
                   .HasForeignKey(e => e.ClassRoomId)
                   .OnDelete(DeleteBehavior.Restrict); // Protect rooms with active enrollments
        }
    }
}
