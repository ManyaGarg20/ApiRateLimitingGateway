using ApiGateway.Strategies;
using Xunit;

namespace ApiGateway.Tests.Strategies;

public class TokenBucketStrategyTests
{
    [Fact]
    public async Task IsAllowedAsync_WhenBucketHasTokens_AllowsRequest()
    {
        var strategy = new TokenBucketStrategy();

        var result = await strategy.IsAllowedAsync("user1:/api/products", capacity: 5, refillRatePerSecond: 1);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task IsAllowedAsync_WhenBucketIsEmpty_RejectsRequest()
    {
        var strategy = new TokenBucketStrategy();
        var key = "user2:/api/products";

        // Exhaust all 3 tokens
        for (int i = 0; i < 3; i++)
        {
            await strategy.IsAllowedAsync(key, capacity: 3, refillRatePerSecond: 0.001);
        }

        var result = await strategy.IsAllowedAsync(key, capacity: 3, refillRatePerSecond: 0.001);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task IsAllowedAsync_WhenTokensRefillOverTime_AllowsRequestAgain()
    {
        var strategy = new TokenBucketStrategy();
        var key = "user3:/api/products";

        // Exhaust the bucket with a fast refill rate
        for (int i = 0; i < 2; i++)
        {
            await strategy.IsAllowedAsync(key, capacity: 2, refillRatePerSecond: 10);
        }

        var rejectedResult = await strategy.IsAllowedAsync(key, capacity: 2, refillRatePerSecond: 10);
        Assert.False(rejectedResult.IsAllowed);

        // Wait long enough for at least 1 token to refill at 10 tokens/sec
        await Task.Delay(200);

        var allowedResult = await strategy.IsAllowedAsync(key, capacity: 2, refillRatePerSecond: 10);
        Assert.True(allowedResult.IsAllowed);
    }

    [Fact]
    public async Task IsAllowedAsync_DifferentUsers_HaveIndependentBuckets()
    {
        var strategy = new TokenBucketStrategy();

        // Exhaust user1's bucket completely
        for (int i = 0; i < 2; i++)
        {
            await strategy.IsAllowedAsync("user1:/api/products", capacity: 2, refillRatePerSecond: 0.001);
        }
        var user1Result = await strategy.IsAllowedAsync("user1:/api/products", capacity: 2, refillRatePerSecond: 0.001);

        // user2 has never made a request - should have a full, fresh bucket
        var user2Result = await strategy.IsAllowedAsync("user2:/api/products", capacity: 2, refillRatePerSecond: 0.001);

        Assert.False(user1Result.IsAllowed);
        Assert.True(user2Result.IsAllowed);
    }

    [Fact]
    public async Task IsAllowedAsync_DifferentEndpoints_HaveIndependentBucketsForSameUser()
    {
        var strategy = new TokenBucketStrategy();

        for (int i = 0; i < 2; i++)
        {
            await strategy.IsAllowedAsync("user1:/api/products", capacity: 2, refillRatePerSecond: 0.001);
        }
        var productsResult = await strategy.IsAllowedAsync("user1:/api/products", capacity: 2, refillRatePerSecond: 0.001);
        var ordersResult = await strategy.IsAllowedAsync("user1:/api/orders", capacity: 2, refillRatePerSecond: 0.001);

        Assert.False(productsResult.IsAllowed);
        Assert.True(ordersResult.IsAllowed);
    }
}
