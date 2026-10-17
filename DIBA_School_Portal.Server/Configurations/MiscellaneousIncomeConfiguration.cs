using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Database_Entities.Configurations
{
    public class MiscellaneousIncomeConfiguration : IEntityTypeConfiguration<MiscellaneousIncome>
    {
        public void Configure(EntityTypeBuilder<MiscellaneousIncome> builder)
        {
            builder.ToTable("MiscellaneousIncomes");
            builder.HasKey(e => e.Id);

            // ---------- Indexes ----------
            builder.HasIndex(e => e.ReferenceNumber).IsUnique();
            builder.HasIndex(e => new { e.AcademicSessionId, e.TermId });
            builder.HasIndex(e => e.RecordedAt);
            builder.HasIndex(e => e.Category);
            builder.HasIndex(e => e.StudentProfileId);

            // ---------- Properties ----------
            builder.Property(e => e.ReferenceNumber).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Category).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
            builder.Property(e => e.Amount).HasPrecision(18, 2);
            builder.Property(e => e.PayerName).HasMaxLength(200);
            builder.Property(e => e.PayerPhone).HasMaxLength(32);
            builder.Property(e => e.Note).HasMaxLength(500);

            // ---------- Relationships (all NoAction) ----------
            builder.HasOne(e => e.StudentProfile)
                   .WithMany()
                   .HasForeignKey(e => e.StudentProfileId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .IsRequired(false);

            builder.HasOne(e => e.AcademicSession)
                   .WithMany()
                   .HasForeignKey(e => e.AcademicSessionId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .IsRequired();

            builder.HasOne(e => e.Term)
                   .WithMany()
                   .HasForeignKey(e => e.TermId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .IsRequired();

            builder.HasOne(e => e.RecordedBy)
                   .WithMany()
                   .HasForeignKey(e => e.RecordedByUserId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .IsRequired(false);
        }
    }
}