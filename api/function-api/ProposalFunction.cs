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
public class ProposalFunction
{
    private readonly IMediator _mediator;

    public ProposalFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [FunctionName("CreateProposal")]
    public async Task<IActionResult> CreateProposal(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "proposals/create")] HttpRequest req,
        ILogger log)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var command = JsonConvert.DeserializeObject<CreateProposalCommand>(body);

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return new OkObjectResult(result.Data);
        else
            return new BadRequestObjectResult(new { result.Message, result.Data });
    }

    [FunctionName("GetProposalById")]
    public async Task<IActionResult> GetProposalById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "proposals/{id}")] HttpRequest req,
        ILogger log)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var command = JsonConvert.DeserializeObject<GetProposalById>(body);

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return new OkObjectResult(result.Data);
        else
            return new BadRequestObjectResult(new { result.Message, result.Data });
    }

}
