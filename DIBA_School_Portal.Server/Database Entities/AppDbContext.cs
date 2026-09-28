using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Configurations
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
       protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PaymentLog>().Property(p => p.Gateway).HasConversion<string>();

            modelBuilder.Entity<PaymentLog>()
                .Property(p => p.Status)
                .HasConversion<string>();
        }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<PaymentLog> PaymentLogs { get;  set; }
    }
   
    
}

