using Microsoft.AspNetCore.Mvc;
using ApiGateway.DTOs;
using ApiGateway.Models;
using ApiGateway.Repositories;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/config/ratelimits")]
public class RateLimitConfigController : ControllerBase
{
    private readonly IRateLimitConfigRepository _repository;

    public RateLimitConfigController(IRateLimitConfigRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<RateLimitConfigDto>>> GetAll()
    {
        var configs = await _repository.GetAllAsync();
        return Ok(configs.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<RateLimitConfigDto>> Create(CreateRateLimitConfigDto dto)
    {
        var config = new RateLimitConfiguration
        {
            Endpoint = dto.Endpoint,
            Capacity = dto.Capacity,
            RefillRatePerSecond = dto.RefillRatePerSecond,
            IsActive = dto.IsActive
        };

        var created = await _repository.CreateAsync(config);
        return Ok(ToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RateLimitConfigDto>> Update(int id, CreateRateLimitConfigDto dto)
    {
        var updated = await _repository.UpdateAsync(id, new RateLimitConfiguration
        {
            Endpoint = dto.Endpoint,
            Capacity = dto.Capacity,
            RefillRatePerSecond = dto.RefillRatePerSecond,
            IsActive = dto.IsActive
        });

        if (updated is null) return NotFound();
        return Ok(ToDto(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private static RateLimitConfigDto ToDto(RateLimitConfiguration c) =>
        new(c.Id, c.Endpoint, c.Capacity, c.RefillRatePerSecond, c.IsActive);
}