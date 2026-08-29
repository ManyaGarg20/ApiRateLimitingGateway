using Microsoft.EntityFrameworkCore;
using ApiGateway.Data;
using ApiGateway.Models;

namespace ApiGateway.Repositories;

public class RateLimitConfigRepository : IRateLimitConfigRepository
{
    private readonly GatewayDbContext _context;

    public RateLimitConfigRepository(GatewayDbContext context)
    {
        _context = context;
    }

    public async Task<List<RateLimitConfiguration>> GetAllAsync()
    {
        return await _context.RateLimitConfigurations.AsNoTracking().ToListAsync();
    }

    public async Task<RateLimitConfiguration?> GetByEndpointAsync(string endpoint)
    {
        return await _context.RateLimitConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Endpoint == endpoint && c.IsActive);
    }

    public async Task<RateLimitConfiguration> CreateAsync(RateLimitConfiguration config)
    {
        _context.RateLimitConfigurations.Add(config);
        await _context.SaveChangesAsync();
        return config;
    }

    public async Task<RateLimitConfiguration?> UpdateAsync(int id, RateLimitConfiguration updated)
    {
        var existing = await _context.RateLimitConfigurations.FindAsync(id);
        if (existing is null) return null;

        existing.Endpoint = updated.Endpoint;
        existing.Capacity = updated.Capacity;
        existing.RefillRatePerSecond = updated.RefillRatePerSecond;
        existing.IsActive = updated.IsActive;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.RateLimitConfigurations.FindAsync(id);
        if (existing is null) return false;

        _context.RateLimitConfigurations.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}