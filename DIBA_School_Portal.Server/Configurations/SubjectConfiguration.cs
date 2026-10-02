using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.HasIndex(x => x.Code)
                   .IsUnique();

            builder.HasMany(x => x.ClassSubjects)
                   .WithOne(cs => cs.Subject)
                   .HasForeignKey(cs => cs.SubjectId)
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }
}