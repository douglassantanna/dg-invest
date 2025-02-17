using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MigrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MigrationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("run")]
    public async Task<ActionResult> RunMigrations([FromBody] RunMigrationsCommand command)
    {

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created("", result);
    }
}
