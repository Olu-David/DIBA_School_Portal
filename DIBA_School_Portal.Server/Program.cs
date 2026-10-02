using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Repository.IServices;
using DIBA_School_Portal.Server.Repository.Services;
using DIBA_School_Portal.Server.Services;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Add Cors policy to allow requests from the frontend application
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", builder =>
    {
        builder.WithOrigins(allowedOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
// Configure Rate Limiter Policies
builder.Services.AddRateLimiter(options =>
{
    // 1. Fixed Window Policy (General API protection)
    options.AddFixedWindowLimiter("FixedPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 2;
    });

    // 2. Sliding Window Policy (Secure / Public endpoints)
    options.AddSlidingWindowLimiter("SlidingPolicy", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 3; // Breaks the window into 3 chunks for smooth rolling
        opt.QueueLimit = 1;
    });

    // 3. Token Bucket Policy (Teacher rapid data entry bursts)
    options.AddTokenBucketLimiter("TokenBucketPolicy", opt =>
    {
        opt.TokenLimit = 20;           // Max bucket capacity
        opt.QueueLimit = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.TokensPerPeriod = 5;       // Adds 5 tokens back every 10 seconds
        opt.AutoReplenishment = true;
    });

    // 4. Concurrency Policy (Heavy PDF / Report generation)
    options.AddConcurrencyLimiter("ConcurrencyPolicy", opt =>
    {
        opt.PermitLimit = 5;           // Max 5 simultaneous heavy requests
        opt.QueueLimit = 10;           // Max 10 requests allowed to wait in line
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Custom Response when a user gets rate limited (HTTP 429)
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync("{\"error\": \"Rate limit exceeded. Please slow down and try again later.\"}");
    };
});
//services
builder.Services.AddScoped<IAuditLog, AuditLogService>();
builder.Services.AddScoped<INotificationLog, NotificationLogService>();
builder.Services.AddScoped<IPaymentLog, PaymentLogService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAcademicSession, AcademicSessionService>();
builder.Services.AddScoped<ISchoolClassService, SchoolClassService>();
builder.Services.AddScoped<ISchoolFeeService, SchoolFeeService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITermSubjectResultService, TermSubjectResultService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
var app = builder.Build();
app.MapHub<DashboardHub>("/dashboardHub").RequireAuthorization();
app.MapDefaultEndpoints();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  
    app.MapOpenApi();

   
    app.MapScalarApiReference();

}
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
