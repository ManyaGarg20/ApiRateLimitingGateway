namespace ApiGateway.Models;

public class TokenBucketState
{
    public double CurrentTokens { get; set; }
    public DateTime LastRefillTimestampUtc { get; set; }
}