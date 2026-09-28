using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ClassLevelConfiguration: IEntityTypeConfiguration<ClassLevel>
    {
        public void Configure(EntityTypeBuilder<ClassLevel> builder)
        {
            builder.HasKey(cl => cl.Id);
            builder.Property(cl => cl.Name).IsRequired().HasMaxLength(50);

            builder.HasMany(cl => cl.ClassRooms)
                   .WithOne(cr => cr.ClassLevel)
                   .HasForeignKey(cr => cr.ClassLevelId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(cl => cl.Fees)
                   .WithOne(sf => sf.ClassLevel)
                   .HasForeignKey(sf => sf.ClassLevelId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
