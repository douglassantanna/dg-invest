using api.Cryptos.Commands;
using api.Cryptos.Queries;
using api.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CryptoController : ControllerBase
{

    private readonly IMediator _mediator;

    public CryptoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create")]
    public async Task<ActionResult<Response>> Create([FromBody] CreateCryptoAssetCommand command)
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

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created("", result);
    }

    [HttpGet("list-assets")]
    public async Task<ActionResult> ListCryptoAssets([FromQuery] ListCryptoAssetsQueryCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("get-crypto-asset-by-id/{CryptoAssetId:int}")]
    public async Task<ActionResult> GetCryptoAssetById([FromRoute] GetCryptoAssetByIdCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}

