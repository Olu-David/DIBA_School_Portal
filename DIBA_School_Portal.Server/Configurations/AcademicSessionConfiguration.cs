using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class AcademicSessionConfiguration : IEntityTypeConfiguration<AcademicSession>
    {
        public void Configure(EntityTypeBuilder<AcademicSession> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
            builder.Property(s => s.IsCurrentSession).IsRequired();

            builder.HasMany(s => s.Terms)
                   .WithOne(t => t.AcademicSession)
                   .HasForeignKey(t => t.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Fees)
                   .WithOne(f => f.AcademicSession)
                   .HasForeignKey(f => f.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Assessments)
                   .WithOne(a => a.AcademicSession)
                   .HasForeignKey(a => a.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
