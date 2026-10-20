using DIBA_School_Portal.Server.Repository.IServices;
using DIBA_School_Portal.Server.Repository.Services.Background;

namespace DIBA_School_Portal.Server.Database_Entities
{
   
        public static class BackgroundServicesExtensions
        {
            public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
            {
                // ---------- Queues (Singleton — shared across requests) ----------
                services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
                services.AddSingleton<IBackgroundSmsQueue, BackgroundSmsQueue>();

                // ---------- Workers (Hosted Services — run continuously) ----------
                services.AddHostedService<EmailWorker>();
                services.AddHostedService<SmsWorker>();

            services.AddHostedService<PromotionAutoRunner>();

            return services;
            }
  }
}
