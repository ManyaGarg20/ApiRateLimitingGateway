namespace ApiGateway.DTOs;

public record CreateRateLimitConfigDto(string Endpoint, int Capacity, double RefillRatePerSecond, bool IsActive);