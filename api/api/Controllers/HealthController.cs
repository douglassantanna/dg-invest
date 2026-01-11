using api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
   private readonly IHealthCheckService _healthCheckService;

    public HealthController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckDatabase(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.IsDatabaseHealthyAsync(cancellationToken);

        return result.IsSuccess
            ? Ok(new { database = "healthy" })
            : StatusCode(503, new { database = "unhealthy", error = result.Error });
    }
}
