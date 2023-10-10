using api.Cryptos.Queries;
using api.Shared;
using api.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost("create")]
    public async Task<ActionResult<Response>> Create([FromBody] CreateUserCommand command)
    {

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created("", result);
    }

    [HttpGet("list-users")]
    public async Task<ActionResult> ListUsers([FromQuery] ListUsersQueryCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("get-user-by-id/{UserId:int}")]
    public async Task<ActionResult> GetUserById([FromRoute] GetUserByIdCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

}
