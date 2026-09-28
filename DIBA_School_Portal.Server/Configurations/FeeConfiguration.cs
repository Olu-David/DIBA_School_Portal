using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class FeeConfiguration : IEntityTypeConfiguration<Fee>
    {
        public void Configure(EntityTypeBuilder<Fee> builder)
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.FeeName).IsRequired().HasMaxLength(100);
            builder.Property(f => f.Amount).HasColumnType("decimal(18,2)").IsRequired();

            builder.HasOne(f => f.ClassLevel)
                   .WithMany(cl => cl.Fees)
                   .HasForeignKey(f => f.ClassLevelId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.AcademicSession)
                   .WithMany(s => s.Fees)
                   .HasForeignKey(f => f.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Term)
                   .WithMany(t => t.Fees)
                   .HasForeignKey(f => f.TermId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
