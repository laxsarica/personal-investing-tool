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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
