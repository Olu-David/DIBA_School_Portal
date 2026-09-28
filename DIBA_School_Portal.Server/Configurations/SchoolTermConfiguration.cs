using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations
{
    public class SchoolTermConfiguration
    {
        public void Configure(EntityTypeBuilder<SchoolTerm> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
            builder.Property(t => t.IsCurrentTerm).IsRequired();

            builder.HasMany(t => t.Fees)
                   .WithOne(sf => sf.Term)
                   .HasForeignKey(sf => sf.TermId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
