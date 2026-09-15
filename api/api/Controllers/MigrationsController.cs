using System.Security.Cryptography;
using System.Text;
using api.Data.Commands;
using api.Data;
using api.Users.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class MigrationsController : ControllerBase
{
    private const string MigrationTokenHeader = "X-Migration-Token";
    private readonly IMediator _mediator;
    private readonly MigrationsSettings _settings;

    public MigrationsController(IMediator mediator, IOptions<MigrationsSettings> settings)
    {
        _mediator = mediator;
        _settings = settings.Value;
    }

    [HttpPost("run")]
    public async Task<ActionResult> RunMigrations([FromHeader(Name = MigrationTokenHeader)] string? migrationToken)
    {
        if (!User.IsInRole(nameof(Role.Admin)) && !HasValidMigrationToken(migrationToken))
        {
            return User.Identity?.IsAuthenticated == true ? Forbid() : Unauthorized();
        }

        var result = await _mediator.Send(new RunMigrationsCommand());
        if (!result.IsSuccess)
        {
            return BadRequest(result.Message);
        }
        return Ok();
    }

    private bool HasValidMigrationToken(string? migrationToken)
    {
        if (!_settings.RemoteTriggerEnabled || string.IsNullOrEmpty(migrationToken) || string.IsNullOrEmpty(_settings.RemoteTriggerToken))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(migrationToken);
        var expectedBytes = Encoding.UTF8.GetBytes(_settings.RemoteTriggerToken);
        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
