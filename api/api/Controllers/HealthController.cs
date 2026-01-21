using api.HealthCheck;
using api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly string _expectedFunctionKey;
    private const string FunctionKeyHeaderName = "X-Function-Key";

    public HealthController(IHealthCheckService healthCheckService, IOptions<HealthPingOptions> options)
    {
        _healthCheckService = healthCheckService;
        _expectedFunctionKey = options.Value.FunctionKey ?? string.Empty;
    }

    [HttpGet("check-database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckDatabase(CancellationToken cancellationToken)
    {
        bool hasKeyHeader = Request.Headers.TryGetValue(FunctionKeyHeaderName, out var providedKey);
        string providedKeyValue = providedKey.ToString();
        bool isExpectedKeyConfigured = !string.IsNullOrWhiteSpace(_expectedFunctionKey);
        bool isKeyValid = hasKeyHeader && string.Equals(providedKeyValue, _expectedFunctionKey, StringComparison.Ordinal);

        if (!isExpectedKeyConfigured)
            return StatusCode(StatusCodes.Status500InternalServerError);

        if (!isKeyValid)
            return Unauthorized();

        var result = await _healthCheckService.IsDatabaseHealthyAsync(cancellationToken);

        return result.IsSuccess
            ? Ok(new { database = "healthy" })
            : StatusCode(503, new { database = "unhealthy", error = result.Error });
    }
}
