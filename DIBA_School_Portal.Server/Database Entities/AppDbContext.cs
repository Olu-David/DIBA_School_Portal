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
        public DbSet<Entities.StudentSubject> StudentSubjects { get; set; }
        public DbSet<PaymentLog> PaymentLogs { get;  set; }
        public DbSet<AcademicSession> AcademicSessions { get; set; }
        public DbSet<StudentPayment> StudentPayments { get; set; }
        public DbSet<SchoolTerm> SchoolTerms { get; set; }
        public DbSet<SchoolClass> ClassLevels { get; set; }  
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }
        public DbSet<ClassSubject> ClassSubjects { get; set; }
        public DbSet<TermResult> TermResults { get; set; }   
        public DbSet<TermSubjectResult> TermSubjectResults {get; set; }
        public DbSet<ParentStudent> ParentStudents { get; set; }
        public DbSet<Fee> Fees { get; set; }
        public DbSet<ClassFeePrice> ClassFeePrices { get; set; }
        public DbSet<ClassBook> ClassBooks { get; set; }

        // Profiles & Enrollments
        public DbSet<Student> Students{ get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<TeacherClass> TeacherClasses { get; set; }
        public DbSet<StudentEnrollments> StudentEnrollments { get; set; }
        public DbSet<ClassFeePrice> ClassFees { get; set; }
        public DbSet<Enquiry> Enquiries { get; set; }
     
    }
   
    
}

