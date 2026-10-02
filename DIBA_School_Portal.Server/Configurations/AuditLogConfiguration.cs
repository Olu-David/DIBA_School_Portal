using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                   .HasMaxLength(450);

            builder.Property(x => x.Action)
                       .HasMaxLength(100);

            builder.Property(x => x.Details)
                       .HasMaxLength(500);

            builder.Property(x => x.IpAddress)
                       .HasMaxLength(45); // IPv6 max length

            builder.Property(x => x.Timestamp)
                       .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
