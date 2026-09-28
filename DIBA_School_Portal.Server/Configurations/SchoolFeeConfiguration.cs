using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class SchoolFeeConfiguration : IEntityTypeConfiguration<Fee>
    {
        public void Configure(EntityTypeBuilder<Fee> builder)
        {
            builder.HasKey(sf => sf.Id);
            builder.Property(sf => sf.FeeName).IsRequired().HasMaxLength(100);
            builder.Property(sf => sf.Description).HasMaxLength(500);
            builder.Property(sf => sf.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(sf => sf.DueDate).IsRequired();
            builder.Property(sf => sf.IsPaid).IsRequired();

            builder.HasOne(sf => sf.ClassLevel)
                   .WithMany(cl => cl.Fees)
                   .HasForeignKey(sf => sf.ClassLevelId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sf => sf.AcademicSession)
                   .WithMany(s => s.Fees)
                   .HasForeignKey(sf => sf.AcademicSessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sf => sf.Term)
                   .WithMany(t => t.Fees)
                   .HasForeignKey(sf => sf.TermId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
