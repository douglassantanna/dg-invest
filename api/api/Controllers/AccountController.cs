using System.Net;
using System.Security.Claims;
using api.Cryptos.Commands;
using api.Shared;
using api.Users.Commands;
using api.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<Response>> GetAccounts()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        GetUserAccountsQuery command = new(userId);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        return Ok(result.Data);
    }

    [HttpPost("create")]
    public async Task<ActionResult<Response>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        CreateAccountCommand command = new(userId, request.SubaccountTag);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            if (result.Data is { } data && data.GetType().GetProperty("HttpStatusCode")?.GetValue(data) is HttpStatusCode httpStatusCode)
            {
                return httpStatusCode switch
                {
                    HttpStatusCode.NotFound => NotFound(result.Message),
                    HttpStatusCode.Conflict => Conflict(result.Message),
                    HttpStatusCode.BadRequest => BadRequest(result.Message),
                    _ => BadRequest(result.Message),
                };
            }
        }
        return Ok(result.Message);
    }

    [HttpGet("account-details")]
    public async Task<ActionResult<Response>> GetAccountDetails()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        GetAccountDetailsQuery command = new(userId);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return NotFound(result.Message);
        return Ok(result.Data);
    }

    [HttpPost("add-crypto-asset")]
    public async Task<ActionResult<Response>> AddCryptoAsset([FromBody] AddCryptoAssetToAccountListRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        var result = await _mediator.Send(new AddCryptoAssetToAccountListCommand(userId, request.CoinMarketCapId, request.Symbol));
        if (!result.IsSuccess)
        {
            if (result.Data is { } data && data.GetType().GetProperty("HttpStatusCode")?.GetValue(data) is HttpStatusCode httpStatusCode)
            {
                return httpStatusCode switch
                {
                    HttpStatusCode.NotFound => NotFound(result),
                    HttpStatusCode.BadRequest => BadRequest(result),
                    _ => BadRequest(result),
                };
            }
        }
        return Created("", result);
    }

    // [HttpPost("add-transaction")]
    // public async Task<ActionResult<Response>> AddTransaction([FromBody] AddTransactionCommand command)
    // {
    //     var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    //     if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
    //     {
    //         return Unauthorized(new Response("Invalid user ID", false));
    //     }

    //     var commandWithUserId = command with { UserId = userId };
    //     var result = await _mediator.Send(commandWithUserId);
    //     if (!result.IsSuccess)
    //     {
    //         return BadRequest(result);
    //     }
    //     return Created("", result);
    // }

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

    [HttpPost("select-account")]
    public async Task<ActionResult<Response>> SelectAccount([FromBody] SelectAccountRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        var result = await _mediator.Send(new SelectAccountCommand(userId, request.AccountId));
        if (!result.IsSuccess)
        {
            return BadRequest(result.Message);
        }
        return Ok();
    }
}
