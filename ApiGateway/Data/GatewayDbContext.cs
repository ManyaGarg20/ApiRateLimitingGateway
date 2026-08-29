using Microsoft.EntityFrameworkCore;
using ApiGateway.Models;

namespace ApiGateway.Data;

public class GatewayDbContext : DbContext
{
    public GatewayDbContext(DbContextOptions<GatewayDbContext> options)
        : base(options)
    {
    }

    public DbSet<RateLimitConfiguration> RateLimitConfigurations => Set<RateLimitConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RateLimitConfiguration>(entity =>
        {
            entity.Property(c => c.Endpoint).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.Endpoint).IsUnique();
        });
    }
}