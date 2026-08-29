namespace ApiGateway.Strategies;

public record RateLimitResult(bool IsAllowed, double RetryAfterSeconds);

public interface IRateLimitStrategy
{
    Task<RateLimitResult> IsAllowedAsync(string key, int capacity, double refillRatePerSecond);
}