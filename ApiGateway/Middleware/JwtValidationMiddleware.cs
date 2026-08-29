using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Middleware;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // Let the dev token endpoint through without a JWT (chicken-and-egg problem otherwise)
if (context.Request.Path.StartsWithSegments("/dev/token") ||
    context.Request.Path.StartsWithSegments("/api/config") ||
    context.Request.Path.StartsWithSegments("/api/stats"))
{
    await _next(context);
    return;
}

        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Request to {Path} missing Bearer token", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid Authorization header" });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var jwtSection = configuration.GetSection("Jwt");

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);

            var userId = principal.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new SecurityTokenException("Token missing userId claim");
            }

            // Stash the validated userId so downstream middleware/controllers can use it
            context.Items["UserId"] = userId;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT validation failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" });
        }
    }
}