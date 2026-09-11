using System.Security.Claims;
using api.AzureKeyVault;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Queries;
using api.Exchanges.Services;
using api.Data;
using api.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExchangeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBybitService _bybitService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;

    public ExchangeController(IMediator mediator, IBybitService bybitService, IKeyVaultService keyVaultService, DataContext context)
    {
        _mediator = mediator;
        _bybitService = bybitService;
        _keyVaultService = keyVaultService;
        _context = context;
    }

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
            return Failure(result);

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
            request.ResolvedExternalId,
            request.Region);

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return Failure(result);

        return Ok(result);
    }

    [HttpPost("bybit/integration-credentials")]
    public async Task<ActionResult<Response>> SaveBybitIntegrationCredentials([FromBody] SaveBybitIntegrationCredentialsRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new SaveBybitIntegrationCredentialsCommand(userId.Value, request.ApiKey, request.ApiSecret, request.Region));
        if (!result.IsSuccess)
            return Failure(result);

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
            return Failure(result);

        return Ok(result);
    }

    // Temporary diagnostic endpoint. Remove after Bybit transfer mapping is verified.
    [HttpGet("bybit/internal-transfers")]
    public async Task<ActionResult<Response>> GetBybitInternalTransfers([FromQuery] int limit = 50, [FromQuery] long? startTime = null)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(x => x.UserId == userId.Value && x.Exchange == "Bybit", HttpContext.RequestAborted);
        if (integration is null || !integration.Enabled)
            return BadRequest(new Response("Bybit integration credentials not found or disconnected", false, 400));

        var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, userId.Value, null, "api-key", HttpContext.RequestAborted);
        var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, userId.Value, null, "api-secret", HttpContext.RequestAborted);
        if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503));
        if (string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
            return BadRequest(new Response("Bybit integration credentials not found", false, 400));

        var transfers = await _bybitService.GetInternalTransferHistoryAsync(
            apiKey.Value, apiSecret.Value, BybitEndpoints.Parse(integration.Region), limit, startTime);
        return Ok(new Response("ok", true, transfers));
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
            return Failure(result);

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
        return result.IsSuccess ? Ok(result) : Failure(result);
    }

    [HttpDelete("bybit/credentials/{accountId}")]
    public async Task<ActionResult<Response>> DeleteCredentials(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new DeleteCredentialsCommand(userId.Value, accountId));
        if (!result.IsSuccess)
            return Failure(result);

        return Ok(result);
    }

    [HttpPost("bybit/disconnect")]
    public async Task<ActionResult<Response>> DisconnectBybitIntegration()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new DisconnectBybitIntegrationCommand(userId.Value));
        if (!result.IsSuccess)
            return Failure(result);

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
        return result.IsSuccess ? Ok(result) : Failure(result);
    }

    [HttpPost("bybit/test-connection/{accountId}")]
    public async Task<ActionResult<Response>> TestBybitConnection(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new Response("Invalid user ID", false));

        var result = await _mediator.Send(new TestBybitConnectionCommand(userId.Value, accountId));
        if (!result.IsSuccess)
            return Failure(result);

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

    private ActionResult<Response> Failure(Response result) =>
        IsKeyVaultUnavailable(result) ? StatusCode(StatusCodes.Status503ServiceUnavailable, result) : BadRequest(result);

    private static bool IsKeyVaultUnavailable(Response result) =>
        result.Data is 503 || string.Equals(result.Data?.ToString(), "503", StringComparison.Ordinal);
}

public record SaveBybitCredentialsRequest(
    int AccountId,
    string ApiKey,
    string ApiSecret,
    string WebhookSecret,
    string? Name = null,
    string? ExternalId = null,
    string? SubaccountTag = null,
    string? BybitUid = null,
    BybitRegion Region = BybitRegion.Global)
{
    public string? ResolvedName => string.IsNullOrWhiteSpace(Name) ? SubaccountTag : Name;
    public string? ResolvedExternalId => string.IsNullOrWhiteSpace(ExternalId) ? BybitUid : ExternalId;
}

public record SaveBybitIntegrationCredentialsRequest(string ApiKey, string ApiSecret, BybitRegion Region = BybitRegion.Global);

public record MapBybitAccountRequest(int AccountId, string? ExternalId = null, string? BybitUid = null)
{
    public string? ResolvedExternalId => string.IsNullOrWhiteSpace(ExternalId) ? BybitUid : ExternalId;
}
