namespace ApiGateway.DTOs;

public record RateLimitConfigDto(int Id, string Endpoint, int Capacity, double RefillRatePerSecond, bool IsActive);