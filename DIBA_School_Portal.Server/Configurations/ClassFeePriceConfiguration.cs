using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ClassFeePriceConfiguration : IEntityTypeConfiguration<ClassFeePrice>
{
    public void Configure(EntityTypeBuilder<ClassFeePrice> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinimumAmountToPay)
               .HasColumnType("decimal(18,2)");

        // Add matching query filter for soft-deleted parent fees
        builder.HasQueryFilter(x => !x.Fee.IsDeleted);

        builder.HasOne(x => x.Fee)
               .WithMany(f => f.ClassFeePrices)
               .HasForeignKey(x => x.FeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SchoolClass)
               .WithMany()
               .HasForeignKey(x => x.SchoolClassId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FeeId, x.SchoolClassId })
               .IsUnique();
    }
}