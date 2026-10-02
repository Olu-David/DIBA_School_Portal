using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class AcademicSessionConfiguration : IEntityTypeConfiguration<AcademicSession>
    {
        public void Configure(EntityTypeBuilder<AcademicSession> builder)
        {

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(x => x.Name)
                   .IsUnique();

            builder.HasMany(x => x.Terms)
                   .WithOne(t => t.AcademicSession)
                   .HasForeignKey(t => t.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Fees)
                   .WithOne(f => f.AcademicSession)
                   .HasForeignKey(f => f.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
