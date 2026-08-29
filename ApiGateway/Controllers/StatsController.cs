using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IRequestStatsService _statsService;

    public StatsController(IRequestStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet]
    public ActionResult<RequestStats> Get()
    {
        return Ok(_statsService.GetStats());
    }
}
