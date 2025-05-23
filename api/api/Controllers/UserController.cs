using System.Net;
using System.Security.Claims;
using api.Cryptos.Queries;
using api.Shared;
using api.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost("create")]
    public async Task<ActionResult<Response>> Create([FromBody] CreateUserCommand request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        CreateUserCommand command = request with { UserCreatorId = userId };
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Created("", result);
    }

    [HttpPost("update-user-password")]
    public async Task<ActionResult<Response>> UpdateUserPassword([FromBody] UpdateUserPasswordCommand command)
    {
        var result = await _mediator.Send(command);
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
        return Ok(result);
    }

    [HttpPost("update-user-profile")]
    public async Task<ActionResult<Response>> UpdateUserProfile([FromBody] UpdateUserProfileCommand command)
    {
        var result = await _mediator.Send(command);
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
        return Ok(result);
    }

    [HttpGet("list-users")]
    public async Task<ActionResult> ListUsers([FromQuery] GetUsersQuery request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        GetUsersQuery command = request with { UserId = userId };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("get-user-by-id")]
    public async Task<ActionResult> GetUserById()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new Response("Invalid user ID", false));
        }

        GetUserByIdQuery command = new(userId);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return NotFound(result.Message);
        return Ok(result);
    }

    [HttpPut("update-user")]
    public async Task<ActionResult> UpdateUser([FromBody] UpdateUserCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            if (result.Data is { } data && data.GetType().GetProperty("HttpStatusCode")?.GetValue(data) is HttpStatusCode httpStatusCode)
            {
                return httpStatusCode switch
                {
                    HttpStatusCode.Unauthorized => Unauthorized(result),
                    HttpStatusCode.NotFound => NotFound(result),
                    HttpStatusCode.BadRequest => BadRequest(result),
                    _ => BadRequest(result),
                };
            }
        }
        return Ok(result);
    }
}
