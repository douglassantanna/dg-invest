using System.Text.Json;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/tradewebhook")]
public class TradeWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TradeWebhookController> _logger;

    public TradeWebhookController(IMediator mediator, ILogger<TradeWebhookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("bybit/{userId:int}/{accountId:int}")]
    public async Task<IActionResult> BybitWebhook(int userId, int accountId)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body))
        {
            rawBody = await reader.ReadToEndAsync();
        }

        var signature = Request.Headers["X-Bybit-Signature"].FirstOrDefault() ?? string.Empty;
        var timestamp = Request.Headers["X-Bybit-Timestamp"].FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        {
            _logger.LogWarning("BybitWebhook: missing signature headers for user {UserId}, account {AccountId}", userId, accountId);
            return Unauthorized();
        }

        BybitWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BybitWebhookPayload>(rawBody);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "BybitWebhook: failed to deserialize payload for user {UserId}, account {AccountId}", userId, accountId);
            return BadRequest();
        }

        if (payload == null)
            return BadRequest();

        var command = new ProcessBybitWebhookCommand(userId, accountId, payload, rawBody, signature, timestamp);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess && result.Data?.ToString() == "401")
            return Unauthorized();

        // Return 200 for all other outcomes so Bybit does not retry indefinitely.
        return Ok();
    }
}
