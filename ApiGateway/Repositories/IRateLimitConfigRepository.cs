using ApiGateway.Models;

namespace ApiGateway.Repositories;

public interface IRateLimitConfigRepository
{
    Task<List<RateLimitConfiguration>> GetAllAsync();
    Task<RateLimitConfiguration?> GetByEndpointAsync(string endpoint);
    Task<RateLimitConfiguration> CreateAsync(RateLimitConfiguration config);
    Task<RateLimitConfiguration?> UpdateAsync(int id, RateLimitConfiguration updated);
    Task<bool> DeleteAsync(int id);
}