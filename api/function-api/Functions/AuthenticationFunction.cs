using System.IO;
using System.Threading.Tasks;
using function_api.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace function_api.Functions;
public class AuthenticationFunction
{
    private readonly IMediator _mediator;

    public AuthenticationFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [FunctionName("Login")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authentication/login")] HttpRequest req,
        ILogger log)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var command = JsonConvert.DeserializeObject<LoginCommand>(body);

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return new OkObjectResult(result.Data);
        else
            return new BadRequestObjectResult(new { result.Message, result.Data });
    }
}
