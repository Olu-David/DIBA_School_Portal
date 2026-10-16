using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace DIBA_School_Portal.Server.Database_Entities
{
 
        public static class RateLimitingExtensions
        {
            public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
            {
                services.AddRateLimiter(options =>
                {
                    // Fixed window — general API protection
                    options.AddFixedWindowLimiter("FixedPolicy", opt =>
                    {
                        opt.PermitLimit = 100;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 2;
                    });

                    // Sliding window — secure / public endpoints
                    options.AddSlidingWindowLimiter("SlidingPolicy", opt =>
                    {
                        opt.PermitLimit = 30;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.SegmentsPerWindow = 3;
                        opt.QueueLimit = 1;
                    });

                    // Token bucket — teacher rapid data entry bursts
                    options.AddTokenBucketLimiter("TokenBucketPolicy", opt =>
                    {
                        opt.TokenLimit = 20;
                        opt.QueueLimit = 5;
                        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                        opt.TokensPerPeriod = 5;
                        opt.AutoReplenishment = true;
                    });

                    // Concurrency — heavy PDF / report generation
                    options.AddConcurrencyLimiter("ConcurrencyPolicy", opt =>
                    {
                        opt.PermitLimit = 5;
                        opt.QueueLimit = 10;
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    });

                    // Custom 429 response
                    options.OnRejected = async (context, cancellationToken) =>
                    {
                        context.HttpContext.Response.StatusCode =
                            StatusCodes.Status429TooManyRequests;
                        context.HttpContext.Response.ContentType = "application/json";

                        await context.HttpContext.Response.WriteAsync(
                            "{\"error\": \"Rate limit exceeded. Please slow down and try again later.\"}",
                            cancellationToken);
                    };
                });

                return services;
            }
        }
  }
