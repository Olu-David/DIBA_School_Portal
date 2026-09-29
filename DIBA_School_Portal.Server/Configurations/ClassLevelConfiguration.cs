using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ClassLevelConfiguration: IEntityTypeConfiguration<SchoolClass>
    {
        public void Configure(EntityTypeBuilder<SchoolClass> builder)
        {
            builder.HasKey(cl => cl.Id);
            builder.Property(cl => cl.Name).IsRequired().HasMaxLength(50);
            builder.Property(cl => cl.DisplayOrder).IsRequired();

            //builder.HasMany(cl => cl.ClassRooms)
            //       .WithOne(cr => cr.ClassLevel)
            //       .HasForeignKey(cr => cr.ClassLevelId)
            //       .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(cl => cl.ClassSubjects)
                   .WithOne(cs => cs.SchoolClass)
                   .HasForeignKey(cs => cs.SchoolClassId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(cl => cl.Fees)
                   .WithOne(f => f.ClassLevel)
                   .HasForeignKey(f => f.ClassLevelId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(cl => cl.Assessments)
                   .WithOne(a => a.ClassLevel)
                   .HasForeignKey(a => a.ClassLevelId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
