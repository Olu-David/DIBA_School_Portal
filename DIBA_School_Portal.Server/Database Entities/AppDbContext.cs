using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Configurations
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
       protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // This scans your assembly and applies every IEntityTypeConfiguration automatically!
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        modelBuilder.Entity<PaymentLog>().Property(p => p.Gateway).HasConversion<string>();

            modelBuilder.Entity<PaymentLog>()
                .Property(p => p.Status)
                .HasConversion<string>();
        }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<PaymentLog> PaymentLogs { get;  set; }
        public DbSet<AcademicSession> AcademicSessions { get; set; }
        public DbSet<SchoolTerm> SchoolTerms { get; set; }
        public DbSet<SchoolClass> ClassLevels { get; set; }  
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }
        public DbSet<ClassSubject> ClassSubjects { get; set; }
        public DbSet<Fee> Fees { get; set; }

        // Profiles & Enrollments
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
    }
   
    
}

