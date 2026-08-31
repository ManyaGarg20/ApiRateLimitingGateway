using ApiGateway.Repositories;
using ApiGateway.Services;
using ApiGateway.Strategies;

namespace ApiGateway.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // Fallback only, used if no configuration exists in Postgres for this endpoint yet
    private const int FallbackCapacity = 10;
    private const double FallbackRefillRatePerSecond = 1.0;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRateLimitStrategy rateLimitStrategy,
        IRateLimitConfigRepository configRepository,
        IRequestStatsService statsService)
    {
        if (context.Request.Path.StartsWithSegments("/dev/token") ||
            context.Request.Path.StartsWithSegments("/api/config") ||
            context.Request.Path.StartsWithSegments("/api/stats"))
        {
            await _next(context);
            return;
        }

        var userId = context.Items["UserId"] as string;
        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing user identity for rate limiting" });
            return;
        }

        var endpoint = context.Request.Path.Value ?? "unknown";

        var config = await configRepository.GetByEndpointAsync(endpoint);
        var capacity = config?.Capacity ?? FallbackCapacity;
        var refillRate = config?.RefillRatePerSecond ?? FallbackRefillRatePerSecond;

        var key = $"{userId}:{endpoint}";
        var result = await rateLimitStrategy.IsAllowedAsync(key, capacity, refillRate);

        if (!result.IsAllowed)
        {
            statsService.RecordRejected();
            _logger.LogWarning("Rate limit exceeded for {Key}", key);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = Math.Ceiling(result.RetryAfterSeconds).ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfterSeconds = Math.Ceiling(result.RetryAfterSeconds)
            });
            return;
        }

        statsService.RecordAllowed();
        await _next(context);
    }
}