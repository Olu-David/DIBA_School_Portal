using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class FeeConfiguration : IEntityTypeConfiguration<Fee>
    {
        public void Configure(EntityTypeBuilder<Fee> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FeeName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Description)
                   .HasMaxLength(500);

            //builder.Property(x => x.Amount)
            //       .HasColumnType("decimal(18,2)");

            //builder.Property(x => x.MinimumAmountToPay)
            //       .HasColumnType("decimal(18,2)");

            // Relationship to ClassLevel (SchoolClass) - first FK
            builder.HasOne(x => x.ClassLevel)
                   .WithMany()
                   .HasForeignKey(x => x.ClassLevelId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship to SchoolClass (second FK)
            builder.HasOne(x => x.SchoolClass)
                   .WithMany(sc => sc.Fees)
                   .HasForeignKey(x => x.SchoolClassId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AcademicSession)
                   .WithMany(a => a.Fees)
                   .HasForeignKey(x => x.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Term)
                   .WithMany(t => t.Fees)
                   .HasForeignKey(x => x.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.ClassFees)
                   .WithOne(cf => cf.Fee)
                   .HasForeignKey(cf => cf.FeeId)
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
