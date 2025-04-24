using api.Data.Commands;
using api.Services.Contracts;
using api.Users.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
// [Authorize(Roles = nameof(Role.Admin))]
[Route("api/[controller]")]
public class MigrationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMarketDataService _marketDataService;

    public MigrationsController(IMediator mediator, IMarketDataService marketDataService)
    {
        _mediator = mediator;
        _marketDataService = marketDataService;
    }

    [HttpPost("run")]
    public async Task<ActionResult> RunMigrations(CancellationToken cancellationToken)
    {
        var result = await _marketDataService.FetchAndProcessMarketDataAsync(cancellationToken);
        // var result = await _mediator.Send(new RunMigrationsCommand());
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }
        return Ok();
    }
}
