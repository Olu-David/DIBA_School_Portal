using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace DIBA_School_Portal.Server.Database_Entities
{
  
        public static class AuthenticationExtensions
        {
            public static IServiceCollection AddJwtAuthentication(
                this IServiceCollection services,
                IConfiguration configuration,
                IWebHostEnvironment environment)
            {
                var jwtKey = configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key is missing.");
                var jwtIssuer = configuration["Jwt:Issuer"]
                    ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
                var jwtAudience = configuration["Jwt:Audience"]
                    ?? throw new InvalidOperationException("Jwt:Audience is missing.");

                if (jwtKey.Length < 32)
                    throw new InvalidOperationException(
                        "Jwt:Key must be at least 32 characters for HMAC-SHA256.");

                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        // HTTPS metadata only enforced in prod
                        options.RequireHttpsMetadata = !environment.IsDevelopment();
                        options.SaveToken = true;

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtIssuer,
                            ValidAudience = jwtAudience,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtKey)),
                            ClockSkew = TimeSpan.Zero,
                            NameClaimType = ClaimTypes.NameIdentifier,
                            RoleClaimType = ClaimTypes.Role,
                        };

                        // Allow SignalR hub to receive token via query string
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];
                                var path = context.HttpContext.Request.Path;

                                if (!string.IsNullOrEmpty(accessToken)
                                    && path.StartsWithSegments("/dashboardHub"))
                                {
                                    context.Token = accessToken;
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });

                return services;
            }
        }
 }


