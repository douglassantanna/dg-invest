using System.Security.Claims;
using api.Exchanges.Commands;
using api.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExchangeController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExchangeController(IMediator mediator) => _mediator = mediator;

    [HttpPost("bybit/credentials")]
    public async Task<ActionResult<Response>> SaveBybitCredentials([FromBody] SaveBybitCredentialsRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var command = new SaveBybitCredentialsCommand(
            userId.Value,
            request.AccountId,
            request.ApiKey,
            request.ApiSecret,
            request.WebhookSecret);

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("bybit/sync-accounts")]
    public async Task<ActionResult<Response>> SyncBybitAccounts()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new SyncBybitAccountsCommand(userId.Value));
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}

public record SaveBybitCredentialsRequest(
    int AccountId,
    string ApiKey,
    string ApiSecret,
    string WebhookSecret);
