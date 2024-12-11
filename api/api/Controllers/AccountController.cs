using System.Security.Claims;
using api.Shared;
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

        GetUserAccountsQueryCommand command = new(userId);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        return Ok(result);
    }
}
