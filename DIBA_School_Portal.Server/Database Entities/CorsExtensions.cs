namespace DIBA_School_Portal.Server.Database_Entities
{
   
        public static class CorsExtensions
        {
            public const string PolicyName = "Frontend";

            public static IServiceCollection AddFrontendCors(
                this IServiceCollection services,
                IConfiguration configuration)
            {
                var allowedOrigins = configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? Array.Empty<string>();

                services.AddCors(options =>
                {
                    options.AddPolicy(PolicyName, policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();   // required for SignalR
                    });
                });

                return services;
            }
        }
  }
