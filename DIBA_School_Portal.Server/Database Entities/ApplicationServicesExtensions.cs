using DIBA_School_Portal.Server.Repository.IServices;
using DIBA_School_Portal.Server.Repository.Services;
using DIBA_School_Portal.Server.Repository.Services.Background;
using DIBA_School_Portal.Server.Services;
using OnlineVotingApplication.Services;

namespace DIBA_School_Portal.Server.Database_Entities
{
  
        public static class ApplicationServicesExtensions
        {
            public static IServiceCollection AddApplicationServices(this IServiceCollection services)
            {
                // ---------- Auth ----------
                services.AddScoped<IAuthService, AuthService>();

                // ---------- Logs ----------
                services.AddScoped<IAuditLog, AuditLogService>();
                services.AddScoped<INotificationLog, NotificationLogService>();
                services.AddScoped<IPaymentLog, PaymentLogService>();

            // ---------- Payments ----------
            services.AddHttpClient<IPaymentService, PaymentService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IExpenseService, ExpenseService>();

            // ---------- Academics ----------
            services.AddScoped<IAcademicSession, AcademicSessionService>();
                services.AddScoped<ISchoolClassService, SchoolClassService>();
                services.AddScoped<ISchoolFeeService, SchoolFeeService>();
                services.AddScoped<ISubjectService, SubjectService>();
                services.AddScoped<ITermSubjectResultService, TermSubjectResultService>();
                services.AddScoped<IStudentService, StudentService>();

                // ---------- Teachers ----------
                services.AddScoped<ITeacherService, TeacherService>();

                // ---------- Parents ----------
                services.AddScoped<IParentService, ParentService>();

                // ---------- Files / PDF / Comms ----------
                services.AddScoped<IPdfReportService, PdfReportService>();
                services.AddScoped<ISupaBaseFileService, SupabaseFileService>();
                services.AddScoped<IEmailService, EmailService>();
                services.AddScoped<ISmsService, SmsService>();

                // ---------- Notifications ----------
                services.AddScoped<INotificationDispatchService, NotificationDispatchService>();

                return services;
            }
        }
  }
