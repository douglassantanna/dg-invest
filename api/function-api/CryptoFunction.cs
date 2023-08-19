using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using function_api.Cryptos.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace function_api;
public class CryptoFunction
{
    private readonly IMediator _mediator;

    public CryptoFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [FunctionName("CreateCryptoAsset")]
    public async Task<IActionResult> CreateCryptoAsset(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "cryptos/create-crypto-asset")] HttpRequest req,
        ILogger log)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var command = JsonConvert.DeserializeObject<CreateCryptoAssetCommand>(body);

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return new OkObjectResult(result.Data);
        else
            return new BadRequestObjectResult(new { result.Message, result.Data });
    }

    [FunctionName("CreateCryptoTransaction")]
    public async Task<IActionResult> CreateCryptoTransaction(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "cryptos/create-crypto-transaction")] HttpRequest req,
        ILogger log)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var command = JsonConvert.DeserializeObject<CreateTransactionCommand>(body);

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return new OkObjectResult(result.Data);
        else
            return new BadRequestObjectResult(new { result.Message, result.Data });
    }

    [FunctionName("GetCryptoAssetById")]
    public async Task<IActionResult> GetCryptoAssetById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "cryptos/id/{id}")] HttpRequest req,
        int id,
        ILogger log)
    {
        var result = await _mediator.Send(new GetCryptoAssetByIdCommand(id));
        if (result.IsSuccess)
            return new OkObjectResult(result.Data);
        else
            return new BadRequestObjectResult(new { result.Message, result.Data });
    }

    [FunctionName("ListCryptoAssets")]
    public async Task<IActionResult> ListCryptoAssets(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "cryptos/list-assets")] HttpRequest req,
    ILogger log)
    {
        var cryptoCurrency = req.Query["cryptoCurrency"];
        var currencyName = req.Query["currencyName"];
        var sortColumn = req.Query["sortColumn"];
        var sortOrder = req.Query["sortOrder"];
        var page = req.Query["page"];
        var pageSize = req.Query["pageSize"];

        log.LogInformation($"cryptoCurrency: {cryptoCurrency}");
        log.LogInformation($"currencyName: {currencyName}");
        log.LogInformation($"sortColumn: {sortColumn}");
        log.LogInformation($"sortOrder: {sortOrder}");
        log.LogInformation($"page: {page}");
        log.LogInformation($"pageSize: {pageSize}");


        ListCryptoAssetsQueryCommand commandWithQuery = new ListCryptoAssetsQueryCommand
        {
            CryptoCurrency = cryptoCurrency.FirstOrDefault() ?? string.Empty,
            CurrencyName = currencyName.FirstOrDefault() ?? string.Empty,
            SortColumn = sortColumn.FirstOrDefault() ?? string.Empty,
            SortOrder = sortOrder.FirstOrDefault() ?? string.Empty,
            Page = TryParseInt(page.FirstOrDefault(), defaultValue: 1),
            PageSize = TryParseInt(pageSize.FirstOrDefault(), defaultValue: 10)
        };

        log.LogInformation($"ListCryptoAssetsQueryCommand: {JsonConvert.SerializeObject(commandWithQuery)}");

        var result = await _mediator.Send(commandWithQuery);

        return new OkObjectResult(result);
    }
    private int TryParseInt(string value, int defaultValue)
    {
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        return defaultValue;
    }
}
