using StackExchange.Redis;

namespace ApiGateway.Strategies;

public class RedisTokenBucketStrategy : IRateLimitStrategy
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _luaScript;
    private const int TtlSeconds = 60;

    public RedisTokenBucketStrategy(IConnectionMultiplexer redis, IWebHostEnvironment env)
    {
        _redis = redis;
        var scriptPath = Path.Combine(env.ContentRootPath, "Scripts", "token_bucket.lua");
        _luaScript = File.ReadAllText(scriptPath);
    }

    public async Task<RateLimitResult> IsAllowedAsync(string key, int capacity, double refillRatePerSecond)
    {
        var db = _redis.GetDatabase();
        var redisKey = $"ratelimit:{key}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = (RedisResult[])(await db.ScriptEvaluateAsync(
            _luaScript,
            keys: new RedisKey[] { redisKey },
            values: new RedisValue[] { capacity, refillRatePerSecond, now, TtlSeconds }
        ))!;

        var allowed = (int)result[0] == 1;
        var retryAfter = (double)result[1];

        return new RateLimitResult(IsAllowed: allowed, RetryAfterSeconds: retryAfter);
    }
}