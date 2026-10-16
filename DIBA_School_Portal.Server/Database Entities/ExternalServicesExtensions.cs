using Amazon.Runtime;
using Amazon.S3;
using Resend;

namespace DIBA_School_Portal.Server.Database_Entities
{
     public static class ExternalServicesExtensions
        {
            public static IServiceCollection AddExternalServices(
                this IServiceCollection services,
                IConfiguration configuration)
            {
                // ---------- HTTP context + SignalR ----------
                services.AddHttpContextAccessor();
                services.AddSignalR();

                // ---------- Distributed cache ----------
                services.AddDistributedMemoryCache();

                // ---------- Resend (email) ----------
                services.AddOptions();
                services.AddHttpClient<IResend, ResendClient>();
                services.Configure<ResendClientOptions>(o =>
                {
                    o.ApiToken = configuration["Resend:ApiKey"]
                               ?? configuration["ResendApiKey"]
                               ?? string.Empty;
                });

                // ---------- AWS S3 (Supabase-compatible) ----------
                services.AddSingleton<IAmazonS3>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var serviceUrl = config["AWS:ServiceUrl"];
                    var accessKey = config["AWS:AccessKey"];
                    var secretKey = config["AWS:SecretKey"];

                    if (string.IsNullOrWhiteSpace(serviceUrl)
                        || string.IsNullOrWhiteSpace(accessKey)
                        || string.IsNullOrWhiteSpace(secretKey))
                    {
                        throw new InvalidOperationException(
                            "AWS S3 settings (ServiceUrl, AccessKey, SecretKey) are missing. " +
                            "Set them in appsettings.json or environment variables.");
                    }

                    var s3Config = new AmazonS3Config
                    {
                        ServiceURL = serviceUrl,
                        ForcePathStyle = true   // required for Supabase S3 compatibility
                    };

                    return new AmazonS3Client(
                        new BasicAWSCredentials(accessKey, secretKey),
                        s3Config);
                });

                return services;
            }
        }
    }
