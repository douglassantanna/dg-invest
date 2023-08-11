using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using function_api.SpotSolar.Commands;
using MediatR;

namespace function_api;
public class Function1
{
    private readonly IMediator _mediator;

    public Function1(IMediator mediator)
    {
        _mediator = mediator;
    }

    [FunctionName("Function1")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var command = JsonConvert.DeserializeObject<CreateProposal>(body);

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return new OkObjectResult(result.Message);
        else
            return new BadRequestObjectResult(result.Message);
    }

}
