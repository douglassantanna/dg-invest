using System.Security.Claims;
using api.Exchanges.Commands;
using api.Exchanges.Queries;
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

    [HttpGet("accounts")]
    public async Task<ActionResult<Response>> GetExchangeAccounts()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetExchangeAccountsQuery(userId.Value));
        return Ok(result);
    }

    [HttpGet("{accountId:int}")]
    public async Task<ActionResult<Response>> GetExchangeAccountDetail(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetExchangeAccountDetailQuery(userId.Value, accountId));
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{accountId:int}/transactions")]
    public async Task<ActionResult<Response>> GetExchangeTransactions(int accountId, [FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetExchangeTransactionsQuery(userId.Value, accountId, limit));
        return Ok(result);
    }

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
            request.WebhookSecret,
            request.ResolvedName,
            request.ResolvedExternalId);

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("bybit/integration-credentials")]
    public async Task<ActionResult<Response>> SaveBybitIntegrationCredentials([FromBody] SaveBybitIntegrationCredentialsRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new SaveBybitIntegrationCredentialsCommand(userId.Value, request.ApiKey, request.ApiSecret));
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

    /// <summary>
    /// Returns all Bybit sub-members with their UIDs and whether they are already
    /// mapped to an app account. Use this to identify which Bybit UID belongs to
    /// which sub-account before calling map-account.
    /// </summary>
    [HttpGet("bybit/sub-members")]
    public async Task<ActionResult<Response>> GetBybitSubMembers()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetBybitSubMembersQuery(userId.Value));
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Manually links an app account to a Bybit sub-account by UID.
    /// Call GET bybit/sub-members first to get the list of UIDs.
    /// </summary>
    [HttpPost("bybit/map-account")]
    public async Task<ActionResult<Response>> MapBybitAccount([FromBody] MapBybitAccountRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var externalId = request.ResolvedExternalId;
        if (string.IsNullOrWhiteSpace(externalId))
            return BadRequest(new Response("External ID is required", false));

        var result = await _mediator.Send(new MapBybitAccountCommand(userId.Value, request.AccountId, externalId));
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("bybit/credentials-status")]
    public async Task<ActionResult<Response>> GetCredentialsStatus()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetCredentialsStatusQuery(userId.Value));
        return Ok(result);
    }

    [HttpDelete("bybit/credentials/{accountId}")]
    public async Task<ActionResult<Response>> DeleteCredentials(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new DeleteCredentialsCommand(userId.Value, accountId));
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("bybit/sync-status")]
    public async Task<ActionResult<Response>> GetSyncStatuses()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetSyncStatusesQuery(userId.Value));
        return Ok(result);
    }

    [HttpGet("bybit/sync-logs/{accountId}")]
    public async Task<ActionResult<Response>> GetSyncLogs(int accountId, [FromQuery] string? date)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetSyncLogsQuery(userId.Value, accountId, date));
        return Ok(result);
    }

    [HttpGet("bybit/connection-groups")]
    public async Task<ActionResult<Response>> GetBybitConnectionGroups()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new GetBybitConnectionGroupQuery(userId.Value));
        return Ok(result);
    }

    [HttpPost("bybit/test-connection/{accountId}")]
    public async Task<ActionResult<Response>> TestBybitConnection(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new TestBybitConnectionCommand(userId.Value, accountId));
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("bybit/toggle/{accountId}")]
    public async Task<ActionResult<Response>> ToggleBybitAccount(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new ToggleBybitAccountCommand(userId.Value, accountId));
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
    string WebhookSecret,
    string? Name = null,
    string? ExternalId = null,
    string? SubaccountTag = null,
    string? BybitUid = null)
{
    public string? ResolvedName => string.IsNullOrWhiteSpace(Name) ? SubaccountTag : Name;
    public string? ResolvedExternalId => string.IsNullOrWhiteSpace(ExternalId) ? BybitUid : ExternalId;
}

public record SaveBybitIntegrationCredentialsRequest(string ApiKey, string ApiSecret);

public record MapBybitAccountRequest(int AccountId, string? ExternalId = null, string? BybitUid = null)
{
    public string? ResolvedExternalId => string.IsNullOrWhiteSpace(ExternalId) ? BybitUid : ExternalId;
}
