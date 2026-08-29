namespace ApiGateway.Models;

public class RateLimitConfiguration
{
    public int Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public double RefillRatePerSecond { get; set; }
    public bool IsActive { get; set; } = true;
}