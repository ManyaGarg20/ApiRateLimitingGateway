using ApiGateway.Data;
using ApiGateway.Middleware;
using ApiGateway.Repositories;
using ApiGateway.Services;
using ApiGateway.Strategies;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var backendBaseUrl = builder.Configuration["BackendApi:BaseUrl"]
    ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured");

builder.Services.AddHttpClient<IProxyService, ProxyService>(client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
});

var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? throw new InvalidOperationException("Redis:ConnectionString is not configured");

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddSingleton<IRateLimitStrategy, RedisTokenBucketStrategy>();

// NEW: Gateway's own Postgres database for configuration
builder.Services.AddDbContext<GatewayDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GatewayDb")));

builder.Services.AddScoped<IRateLimitConfigRepository, RateLimitConfigRepository>();

// NEW: CORS, needed for Phase 8's React dashboard running on a different port
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowDashboard");

app.UseMiddleware<JwtValidationMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();