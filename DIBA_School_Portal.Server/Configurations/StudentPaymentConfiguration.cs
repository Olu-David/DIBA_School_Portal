using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class StudentPaymentItemConfiguration : IEntityTypeConfiguration<StudentPaymentItem>
    {
        public void Configure(EntityTypeBuilder<StudentPaymentItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.StudentPayment)
                   .WithMany(p => p.PaymentItems)
                   .HasForeignKey(x => x.StudentPaymentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ★ Optional relationship — LEFT JOIN, payment item survives fee filter
            builder.HasOne(x => x.Fee)
                   .WithMany()
                   .HasForeignKey(x => x.FeeId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.HasIndex(x => x.StudentPaymentId);
            builder.HasIndex(x => x.FeeId);
        }
    }
}