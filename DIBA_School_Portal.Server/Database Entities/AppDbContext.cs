using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Database_Entities.Configurations;
using DIBA_School_Portal.Server.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Database_Entities
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // ---- Academy ----
        public DbSet<AcademicSession> AcademicSessions { get; set; }
        public DbSet<SchoolTerm> SchoolTerms { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ClassSubject> ClassSubjects { get; set; }

        // ---- Results ----
        public DbSet<TermResult> TermResults { get; set; }
        public DbSet<TermSubjectResult> TermSubjectResults { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }

        // ---- Finance ----
        public DbSet<Fee> Fees { get; set; }
        public DbSet<ClassFeePrice> ClassFeePrices { get; set; }
        public DbSet<StudentPaymentItem> StudentPaymentItems { get; set; }
        public DbSet<StudentPayment> StudentPayments { get; set; }
        public DbSet<PaymentLog> PaymentLogs { get; set; }
        public DbSet<MiscellaneousIncome> MiscellaneousIncomes { get; set; }
        public DbSet<Expense> Expenses { get; set; }    

        // ---- People ----
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<TeacherClass> TeacherClasses { get; set; }
        public DbSet<StudentEnrollment> StudentEnrollments { get; set; }
        public DbSet<ParentStudent> ParentStudents { get; set; }

        // ---- System ----
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<Enquiry> Enquiries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Auto-discovery for all separate configuration files
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Explicit registration for MiscellaneousIncome configuration
            modelBuilder.ApplyConfiguration(new MiscellaneousIncomeConfiguration());

            // Global configuration to map all decimals to PostgreSQL numeric type safely
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("numeric(18,2)");
            }

            // Enum-to-string conversions
            modelBuilder.Entity<PaymentLog>()
                .Property(p => p.Gateway)
                .HasConversion<string>();

            modelBuilder.Entity<PaymentLog>()
                .Property(p => p.Status)
                .HasConversion<string>();
        }
    }
}