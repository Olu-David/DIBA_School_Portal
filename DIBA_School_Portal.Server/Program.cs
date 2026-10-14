using Amazon.Runtime;
using Amazon.S3;
using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using DIBA_School_Portal.Server.Repository.Services;
using DIBA_School_Portal.Server.Repository.Services.Background;
using DIBA_School_Portal.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OnlineVotingApplication.Services;
using Resend;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ==========================================================
// 1. DATABASE — SQL Server LocalDB
// ==========================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    ));

// ==========================================================
// 2. IDENTITY
// ==========================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User
    options.User.RequireUniqueEmail = false; // students have no email
})
.AddRoles<IdentityRole>() // <--- ADD THIS LINE HERE
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
// ==========================================================
// 3. HTTP CONTEXT ACCESSOR + SIGNALR
// ==========================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

// ==========================================================
// 4. OPENAPI + CONTROLLERS
// ==========================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ==========================================================
// 5. CORS
// ==========================================================
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();   // needed for SignalR + cookies
    });
});

// ==========================================================
// 6. RATE LIMITER
// ==========================================================
builder.Services.AddRateLimiter(options =>
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
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\": \"Rate limit exceeded. Please slow down and try again later.\"}",
            cancellationToken);
    };
});

// ==========================================================
// 7. EXTERNAL DEPENDENCIES (required by imported services)
// ==========================================================

// --- Distributed Cache (needed by AcademicSessionService) ---
builder.Services.AddDistributedMemoryCache();

// --- Resend email client (needed by EmailService) ---
builder.Services.AddOptions();
builder.Services.AddHttpClient<IResend, ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Resend:ApiKey"]
               ?? builder.Configuration["ResendApiKey"]
               ?? string.Empty;
});

// --- AWS S3 (needed by SupabaseFileService) ---
builder.Services.AddSingleton<IAmazonS3>(sp =>
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
        ForcePathStyle = true   // Required for Supabase S3 compatibility
    };

    return new AmazonS3Client(
        new BasicAWSCredentials(accessKey, secretKey),
        s3Config);
});
// ==========================================================
// 8. JWT 
// ==========================================================

var jwtKey = builder.Configuration["Jwt:Key"] ?? "THIS_IS_A_VERY_LONG_SECRET_KEY_THAT_IS_AT_LEAST_32_CHARACTERS_LONG_FOR_HMAC_SHA256";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DIBA_School_Portal";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DIBA_School_Portal_Clients";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;              // dev only — set true in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,                     // no grace period on expiry
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };

        // Allow the SignalR hub to receive the token via query string
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
// ==========================================================
// 9. APPLICATION SERVICES
// ==========================================================

// --- Auth ---
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Logs ---
builder.Services.AddScoped<IAuditLog, AuditLogService>();
builder.Services.AddScoped<INotificationLog, NotificationLogService>();
builder.Services.AddScoped<IPaymentLog, PaymentLogService>();

// --- Payments ---
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAccountService  , AccountService>();   
// --- Academics ---
builder.Services.AddScoped<IAcademicSession, AcademicSessionService>();
builder.Services.AddScoped<ISchoolClassService, SchoolClassService>();
builder.Services.AddScoped<ISchoolFeeService, SchoolFeeService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITermSubjectResultService, TermSubjectResultService>();
builder.Services.AddScoped<IStudentService, StudentService>();

// --- Teachers ---
builder.Services.AddScoped<ITeacherService, TeacherService>();  

// --- Parents ---
builder.Services.AddScoped<IParentService, ParentService>();
// --- Parents ---
builder.Services.AddScoped<IParentService, ParentService>();

// --- Files ---
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<ISupaBaseFileService, SupabaseFileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();        // ← ADD if not there// --- External ---
builder.Services.AddScoped<ISupaBaseFileService, SupabaseFileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
// And on Kestrel(or IIS) — cap the raw request body too
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2 * 1024 * 1024;   // 2 MB headroom
});// ==========================================================
// Background queues (Singleton — shared across requests)
// ==========================================================
builder.Services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
builder.Services.AddSingleton<IBackgroundSmsQueue, BackgroundSmsQueue>();

// ==========================================================
// Background workers (Hosted Services — run continuously)
// ==========================================================
builder.Services.AddHostedService<EmailWorker>();
builder.Services.AddHostedService<SmsWorker>();

// ==========================================================
// Notification dispatch service
// ==========================================================
builder.Services.AddScoped<INotificationDispatchService, NotificationDispatchService>();
// ==========================================================
// 10. BUILD
// ==========================================================
var app = builder.Build();

// ==========================================================
// 11. DATABASE MIGRATION + SEEDING
// ==========================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during migration/seeding.");
    }
}

// ==========================================================
// 12. MIDDLEWARE PIPELINE (order matters!)
// ==========================================================

// Static files + default files (must be early)
app.UseDefaultFiles();
app.MapStaticAssets();

// Development only — OpenAPI + Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// CORS before auth
app.UseCors("Frontend");

// Rate limiting before auth (protect login endpoints too)
app.UseRateLimiter();

// Redirect HTTP → HTTPS
app.UseHttpsRedirection();

// WHO you are
app.UseAuthentication();

// WHAT you can do
app.UseAuthorization();

// Controllers
app.MapControllers();

// SignalR hub (after auth so [Authorize] on Hub works)
app.MapHub<DashboardHub>("/dashboardHub")
   .RequireAuthorization();

// Service defaults (Aspire)
app.MapDefaultEndpoints();

// SPA fallback (must be last)
app.MapFallbackToFile("/index.html");

// Start the server
app.Run();
