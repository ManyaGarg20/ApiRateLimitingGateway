using ApiGateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace ApiGateway.Tests.Middleware;

public class JwtValidationMiddlewareTests
{
    private static IConfiguration BuildTestConfig()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:SigningKey", "TEST_ONLY_SIGNING_KEY_32_BYTES_MINIMUM_LENGTH" },
            { "Jwt:ExpiryMinutes", "60" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private static string GenerateTestToken(IConfiguration config, string userId, bool expired = false)
    {
        var jwtSection = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: new[] { new Claim("userId", userId) },
            expires: expired ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task InvokeAsync_NoAuthorizationHeader_Returns401()
    {
        var config = BuildTestConfig();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new JwtValidationMiddleware(next, Mock.Of<ILogger<JwtValidationMiddleware>>());
        await middleware.InvokeAsync(context, config);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ValidToken_CallsNextAndSetsUserId()
    {
        var config = BuildTestConfig();
        var token = GenerateTestToken(config, "user123");

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new JwtValidationMiddleware(next, Mock.Of<ILogger<JwtValidationMiddleware>>());
        await middleware.InvokeAsync(context, config);

        Assert.True(nextCalled);
        Assert.Equal("user123", context.Items["UserId"]);
    }

    [Fact]
    public async Task InvokeAsync_ExpiredToken_Returns401()
    {
        var config = BuildTestConfig();
        var token = GenerateTestToken(config, "user123", expired: true);

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new JwtValidationMiddleware(next, Mock.Of<ILogger<JwtValidationMiddleware>>());
        await middleware.InvokeAsync(context, config);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_DevTokenPath_BypassesValidation()
    {
        var config = BuildTestConfig();
        var context = new DefaultHttpContext();
        context.Request.Path = "/dev/token";
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new JwtValidationMiddleware(next, Mock.Of<ILogger<JwtValidationMiddleware>>());
        await middleware.InvokeAsync(context, config);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode); // default, untouched
    }
}
