using ApiGateway.Middleware;
using ApiGateway.Repositories;
using ApiGateway.Services;
using ApiGateway.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiGateway.Tests.Middleware;

public class RateLimitMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoUserId_Returns401AndDoesNotCallStrategy()
    {
        var mockStrategy = new Mock<IRateLimitStrategy>();
        var mockConfigRepo = new Mock<IRateLimitConfigRepository>();
        var mockStats = new Mock<IRequestStatsService>();

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        // No context.Items["UserId"] set - simulates JWT middleware not having run/failed

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new RateLimitMiddleware(next, Mock.Of<ILogger<RateLimitMiddleware>>());
        await middleware.InvokeAsync(context, mockStrategy.Object, mockConfigRepo.Object, mockStats.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
        mockStrategy.Verify(s => s.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_StrategyAllows_CallsNextAndRecordsAllowed()
    {
        var mockStrategy = new Mock<IRateLimitStrategy>();
        mockStrategy
            .Setup(s => s.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
            .ReturnsAsync(new RateLimitResult(IsAllowed: true, RetryAfterSeconds: 0));

        var mockConfigRepo = new Mock<IRateLimitConfigRepository>();
        mockConfigRepo
            .Setup(r => r.GetByEndpointAsync(It.IsAny<string>()))
            .ReturnsAsync((ApiGateway.Models.RateLimitConfiguration?)null); // use fallback defaults

        var mockStats = new Mock<IRequestStatsService>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        context.Items["UserId"] = "user123";
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new RateLimitMiddleware(next, Mock.Of<ILogger<RateLimitMiddleware>>());
        await middleware.InvokeAsync(context, mockStrategy.Object, mockConfigRepo.Object, mockStats.Object);

        Assert.True(nextCalled);
        mockStats.Verify(s => s.RecordAllowed(), Times.Once);
        mockStats.Verify(s => s.RecordRejected(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_StrategyRejects_Returns429AndDoesNotCallNext()
    {
        var mockStrategy = new Mock<IRateLimitStrategy>();
        mockStrategy
            .Setup(s => s.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
            .ReturnsAsync(new RateLimitResult(IsAllowed: false, RetryAfterSeconds: 3.5));

        var mockConfigRepo = new Mock<IRateLimitConfigRepository>();
        mockConfigRepo
            .Setup(r => r.GetByEndpointAsync(It.IsAny<string>()))
            .ReturnsAsync((ApiGateway.Models.RateLimitConfiguration?)null);

        var mockStats = new Mock<IRequestStatsService>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        context.Items["UserId"] = "user123";
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new RateLimitMiddleware(next, Mock.Of<ILogger<RateLimitMiddleware>>());
        await middleware.InvokeAsync(context, mockStrategy.Object, mockConfigRepo.Object, mockStats.Object);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.False(nextCalled);
        mockStats.Verify(s => s.RecordRejected(), Times.Once);
        mockStats.Verify(s => s.RecordAllowed(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_UsesConfigFromRepository_WhenAvailable()
    {
        var mockStrategy = new Mock<IRateLimitStrategy>();
        mockStrategy
            .Setup(s => s.IsAllowedAsync(It.IsAny<string>(), 5, 0.5))
            .ReturnsAsync(new RateLimitResult(IsAllowed: true, RetryAfterSeconds: 0));

        var mockConfigRepo = new Mock<IRateLimitConfigRepository>();
        mockConfigRepo
            .Setup(r => r.GetByEndpointAsync("/api/products"))
            .ReturnsAsync(new ApiGateway.Models.RateLimitConfiguration
            {
                Id = 1,
                Endpoint = "/api/products",
                Capacity = 5,
                RefillRatePerSecond = 0.5,
                IsActive = true
            });

        var mockStats = new Mock<IRequestStatsService>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        context.Items["UserId"] = "user123";
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RateLimitMiddleware(next, Mock.Of<ILogger<RateLimitMiddleware>>());
        await middleware.InvokeAsync(context, mockStrategy.Object, mockConfigRepo.Object, mockStats.Object);

        // Verifies the middleware passed the DB-sourced capacity=5, refillRate=0.5 - not the hardcoded fallback
        mockStrategy.Verify(s => s.IsAllowedAsync(It.IsAny<string>(), 5, 0.5), Times.Once);
    }
}
