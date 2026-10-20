using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace DIBA_School_Portal.Server.Configurations
{
    public class SchoolClassConfiguration: IEntityTypeConfiguration<SchoolClass>
    {
        public void Configure(EntityTypeBuilder<SchoolClass> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(x => x.Name)
                   .IsUnique();

            builder.HasMany(x => x.ClassSubjects)
                   .WithOne(cs => cs.SchoolClass)
                   .HasForeignKey(cs => cs.SchoolClassId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Fees)
                   .WithOne(f => f.SchoolClass)
                   .HasForeignKey(f => f.SchoolClassId)
                   .OnDelete(DeleteBehavior.Restrict);




        }
    }
}
