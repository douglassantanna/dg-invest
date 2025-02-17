using api.Data.Commands;
using api.Users.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Admin))]
[Route("api/[controller]")]
public class MigrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MigrationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("run")]
    public async Task<ActionResult> RunMigrations()
    {
        var result = await _mediator.Send(new RunMigrationsCommand());
        if (!result.IsSuccess)
        {
            return BadRequest(result.Message);
        }
        return Ok();
    }
}
