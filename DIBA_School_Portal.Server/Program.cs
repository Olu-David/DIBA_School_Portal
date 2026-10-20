using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Database_Entities;

var builder = WebApplication.CreateBuilder(args);

// ---------- Aspire service defaults ----------
builder.AddServiceDefaults();

// ==========================================================
// SERVICES
// ==========================================================
builder.Services
    .AddInfrastructureServices(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration, builder.Environment)
    .AddFrontendCors(builder.Configuration)
    .AddApiLayer()
    .AddAppRateLimiting()
    .AddExternalServices(builder.Configuration)
    .AddApplicationServices()
    .AddBackgroundServices();
builder.Services.Configure<FlutterwaveSettings>(
    builder.Configuration.GetSection("Flutterwave"));

// ---------- Kestrel request body limit ----------
builder.WebHost.AddKestrelLimits(builder.Configuration);

var app = builder.Build();

// ==========================================================
// MIGRATE + SEED
// ==========================================================
//await app.MigrateAndSeedAsync();

// ==========================================================
// PIPELINE
// ==========================================================
await app.UseAppPipelineAsync();

app.Run();