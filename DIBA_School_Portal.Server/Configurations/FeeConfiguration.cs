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

            builder.Property(x => x.IsDeleted)
                   .HasDefaultValue(false);

            // Global query filter for soft delete
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Only configure the collection relationship here (foreign keys are handled by annotations on the properties)
            builder.HasMany(x => x.ClassFeePrices)
                   .WithOne(cp => cp.Fee)
                   .HasForeignKey(cp => cp.FeeId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}