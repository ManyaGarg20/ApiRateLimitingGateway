using System.Collections.Concurrent;
using ApiGateway.Models;

namespace ApiGateway.Strategies;

public class TokenBucketStrategy : IRateLimitStrategy
{
    // key = "userId:endpoint", value = that bucket's current state
    private readonly ConcurrentDictionary<string, TokenBucketState> _buckets = new();
    private readonly object _lockObject = new();

    public Task<RateLimitResult> IsAllowedAsync(string key, int capacity, double refillRatePerSecond)
    {
        var now = DateTime.UtcNow;

        // Lock per-check to keep the read-modify-write atomic.
        // A single global lock is fine at our scale (this whole limitation goes away in Phase 6 with Redis).
        lock (_lockObject)
        {
            var bucket = _buckets.GetOrAdd(key, _ => new TokenBucketState
            {
                CurrentTokens = capacity,
                LastRefillTimestampUtc = now
            });

            // Lazy refill: compute how many tokens should have accumulated since last check
            var elapsedSeconds = (now - bucket.LastRefillTimestampUtc).TotalSeconds;
            var tokensToAdd = elapsedSeconds * refillRatePerSecond;

            bucket.CurrentTokens = Math.Min(capacity, bucket.CurrentTokens + tokensToAdd);
            bucket.LastRefillTimestampUtc = now;

            if (bucket.CurrentTokens >= 1)
            {
                bucket.CurrentTokens -= 1;
                return Task.FromResult(new RateLimitResult(IsAllowed: true, RetryAfterSeconds: 0));
            }

            // Not enough tokens - compute how long until 1 token will be available
            var tokensNeeded = 1 - bucket.CurrentTokens;
            var retryAfterSeconds = tokensNeeded / refillRatePerSecond;

            return Task.FromResult(new RateLimitResult(IsAllowed: false, RetryAfterSeconds: retryAfterSeconds));
        }
    }
}