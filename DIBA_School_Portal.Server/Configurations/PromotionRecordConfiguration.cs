using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{

        public class PromotionRecordConfiguration : IEntityTypeConfiguration<PromotionRecord>
        {
            public void Configure(EntityTypeBuilder<PromotionRecord> builder)
            {
                builder.HasKey(x => x.Id);

                builder.HasOne(x => x.Student)
                       .WithMany()
                       .HasForeignKey(x => x.StudentId)
                       .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(x => x.FromClass)
                       .WithMany()
                       .HasForeignKey(x => x.FromClassId)
                       .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(x => x.ToClass)
                       .WithMany()
                       .HasForeignKey(x => x.ToClassId)
                       .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(x => x.AcademicSession)
                       .WithMany()
                       .HasForeignKey(x => x.AcademicSessionId)
                       .OnDelete(DeleteBehavior.Restrict);

                builder.HasIndex(x => x.BatchId);
                builder.HasIndex(x => x.StudentId);
                builder.HasIndex(x => new { x.AcademicSessionId, x.PromotedAt });
            }
        }
  }

