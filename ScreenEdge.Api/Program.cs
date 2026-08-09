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
using Hangfire.SqlServer;
using ScreenEdge.Api.Jobs;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Hangfire Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true
    }));

// Add the Hangfire processing server
builder.Services.AddHangfireServer();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost", "http://localhost:8080")
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
RecurringJob.AddOrUpdate<ScreenerJob>(
    "daily-data-sync",
    job => job.RunDailyDataSyncAsync(),
    "0 1 * * 1-5" // 6:30 AM IST (1:00 AM UTC), Monday to Friday
);

RecurringJob.AddOrUpdate<ScreenerJob>(
    "daily-screener-run",
    job => job.RunScreenerOnlyAsync(),
    "30 1 * * 1-5" // 7:00 AM IST (1:30 AM UTC), Monday to Friday
);

app.Run();
