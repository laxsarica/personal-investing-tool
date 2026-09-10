using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ScreenEdge.Entity;
using ScreenEdge.Repository;
using ScreenEdge.Screener;
using ScreenEdge.Broker;
using ScreenEdge.Api.Services;
using ScreenEdge.Broker.Kite;
using Hangfire;
using Hangfire.PostgreSql;
using ScreenEdge.Api.Jobs;

// Enable Npgsql legacy timestamp behavior for seamless DateTime compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

Console.WriteLine("[Startup] Initializing ScreenEdge API...");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Explicitly bind Kestrel to 127.0.0.1:5000 so container ASPNETCORE_HTTP_PORTS does not redirect to 8080
builder.WebHost.UseUrls("http://127.0.0.1:5000");

// Resolve PostgreSQL Connection String (supports both ADO.NET and URI format)
var connectionString = ResolvePostgresConnectionString(builder.Configuration);

// Register IHttpClientFactory (used by NewsController to proxy TradingView news)
builder.Services.AddHttpClient();

// Add Yahoo Finance service (uses static YahooFinanceApi library - no HttpClient needed)
builder.Services.AddScoped<YahooFinanceService>();

// EF Core (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repository + UoW
builder.Services.AddScoped<IUnitOfWorks, UnitOfWorks>();

// Screener Engine
builder.Services.AddScoped<IScreenerEngine, ScreenerEngine>();

// Broker Data Ingestion
builder.Services.Configure<BrokerSettings>(builder.Configuration.GetSection("BrokerSettings"));
builder.Services.AddSingleton<TradeApiCreds>();
builder.Services.AddScoped<IBrokerPortfolioProvider, AngelOnePortfolioProvider>();
builder.Services.AddScoped<IBrokerPortfolioProvider, KitePortfolioProvider>();
builder.Services.AddScoped<DataIngestionService>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ScreenEdge-Super-Secret-Key-2025-Do-Not-Share!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "ScreenEdge",
            ValidAudience = "ScreenEdge",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Hangfire Configuration (PostgreSQL)
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
    {
        QueuePollInterval = TimeSpan.FromSeconds(15),
        InvisibilityTimeout = TimeSpan.FromMinutes(5)
    }));

// Add the Hangfire processing server
builder.Services.AddHangfireServer();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:4200", 
                  "http://localhost", 
                  "http://localhost:8080",
                  "https://screener.getvoroa.com"
              )
              .SetIsOriginAllowed(origin =>
                  string.IsNullOrEmpty(origin) ||
                  origin.EndsWith(".getvoroa.com", StringComparison.OrdinalIgnoreCase) ||
                  origin.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase))
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (No authentication configured for local use)
app.UseHangfireDashboard("/hangfire");

app.MapControllers();

// Register Hangfire Recurring Jobs

// Remove legacy standalone jobs
RecurringJob.RemoveIfExists("daily-data-sync");
RecurringJob.RemoveIfExists("daily-screener-run");
RecurringJob.RemoveIfExists("weekly-finnhub-sync"); // Replaced by Yahoo Finance

// Single authoritative daily job: sync data → run screener (Mon–Fri, 6:30 AM IST)
RecurringJob.AddOrUpdate<ScreenerJob>(
    "daily-screener-workflow",
    job => job.RunDailyWorkflowAsync(),
    "0 1 * * 1-5" // 6:30 AM IST (1:00 AM UTC), Monday to Friday
);

RecurringJob.AddOrUpdate<FundamentalsSyncJob>(
    "weekly-fundamentals-sync", 
    x => x.SyncFundamentalsAsync(), 
    Cron.Weekly(DayOfWeek.Saturday, 2)
); // Runs every Saturday at 2 AM UTC

// Automatically apply any pending EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        Console.WriteLine("[Startup] Checking and applying database migrations...");
        logger.LogInformation("Checking and applying pending database migrations...");
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("[Startup] Database migrations applied successfully.");
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup ERROR] Migration failed: {ex.Message}");
        logger.LogError(ex, "An error occurred while applying EF Core database migrations.");
    }
}

Console.WriteLine("[Startup] Kestrel ready. Starting server on http://127.0.0.1:5000...");
app.Run();

// Helper method to resolve and normalize PostgreSQL connection strings
static string ResolvePostgresConnectionString(IConfiguration configuration)
{
    var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? Environment.GetEnvironmentVariable("POSTGRES_URL")
        ?? Environment.GetEnvironmentVariable("POSTGRESQL_URL")
        ?? Environment.GetEnvironmentVariable("DB_URL")
        ?? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? configuration["DATABASE_URL"]
        ?? configuration["DefaultConnection"];

    if (string.IsNullOrWhiteSpace(raw))
    {
        throw new InvalidOperationException(
            "PostgreSQL connection string not configured. " +
            "Please set the DATABASE_URL (or POSTGRES_URL) environment variable, " +
            "or ensure ConnectionStrings:DefaultConnection is set in appsettings.json.");
    }

    raw = raw.Trim().Trim('"', '\'');

    // Handle URI format: postgres://user:password@host:port/database?sslmode=require
    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        var conn = $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Timeout=60;";
        Console.WriteLine($"[Startup] Resolved PostgreSQL URI -> Host: {uri.Host}, Port: {port}, Database: {database}, User: {username}");
        return conn;
    }

    Console.WriteLine("[Startup] Resolved standard ADO.NET PostgreSQL connection string.");
    return raw;
}
