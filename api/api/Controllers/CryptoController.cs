using System.Security.Claims;
using api.Cryptos.Commands;
using api.Cryptos.Queries;
using api.RateLimiterPolicies;
using api.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CryptoController : ControllerBase
{
    private readonly IMediator _mediator;
    public CryptoController(IMediator mediator) => _mediator = mediator;

    [HttpPost("create")]
    public async Task<ActionResult<Response>> Create([FromBody] AddCryptoAssetToUserListCommand command)
    {

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created("", result);
    }

    [HttpPost("add-transaction")]
    public async Task<ActionResult<Response>> AddTransaction([FromBody] AddTransactionCommand command)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        var commandWithUserId = command with { UserId = userId };
        var result = await _mediator.Send(commandWithUserId);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created("", result);
    }

    [HttpPost("deposit-fund")]
    public async Task<ActionResult<Response>> Depositfund([FromBody] DepositFundCommand command)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        var commandWithUserId = command with { UserId = userId };
        var result = await _mediator.Send(commandWithUserId);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created(nameof(DepositFundCommand), result);
    }

    [HttpPost("withdraw-fund")]
    public async Task<ActionResult<Response>> Withdrawfund([FromBody] WithdrawFundCommand command)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        var commandWithUserId = command with { UserId = userId };
        var result = await _mediator.Send(commandWithUserId);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created(nameof(DepositFundCommand), result);
    }

    [HttpGet("list-assets")]
    public async Task<ActionResult> ListCryptoAssets([FromQuery] ListCryptoAssetsQueryCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [EnableRateLimiting(ServiceExtensions.DefaultPolicy)]
    [HttpGet("get-crypto-asset-by-id/{CryptoAssetId:int}")]
    public async Task<ActionResult> GetCryptoAssetById([FromRoute] GetCryptoAssetByIdCommandQuery command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
    [HttpGet("get-cryptos")]
    public async Task<ActionResult> GetCryptos([FromRoute] GetCryptosCommandQuery command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

