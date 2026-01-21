using api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly string _expectedFunctionKey;
    private const string FunctionKeyHeaderName = "X-Function-Key";

    public HealthController(IHealthCheckService healthCheckService, IConfiguration configuration)
    {
        _healthCheckService = healthCheckService;
        _expectedFunctionKey = configuration["HealthCheck:FunctionKey"] ?? string.Empty;
    }

    [HttpGet("check-database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckDatabase(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_expectedFunctionKey))
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Health check key not configured." });

        if (!Request.Headers.TryGetValue(FunctionKeyHeaderName, out var providedKey) ||
            !string.Equals(providedKey.ToString(), _expectedFunctionKey, StringComparison.Ordinal))
            return Unauthorized();

        var result = await _healthCheckService.IsDatabaseHealthyAsync(cancellationToken);

        return result.IsSuccess
            ? Ok(new { database = "healthy" })
            : StatusCode(503, new { database = "unhealthy", error = result.Error });
    }
}
