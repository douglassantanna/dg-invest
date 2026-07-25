using api.AzureKeyVault;
using api.CoinMarketCap.Service;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetReconciliationQuery(int UserId, int AccountId) : IRequest<Response>;

public record ReconciliationDto(
    decimal BybitTotal,
    decimal AppTotal,
    decimal Drift);

public class GetReconciliationQueryHandler : IRequestHandler<GetReconciliationQuery, Response>
{
    private readonly DataContext _context;
    private readonly IKeyVaultService _keyVaultService;
    private readonly IBybitService _bybitService;
    private readonly ICoinMarketCapService _cmcService;

    public GetReconciliationQueryHandler(
        DataContext context,
        IKeyVaultService keyVaultService,
        IBybitService bybitService,
        ICoinMarketCapService cmcService)
    {
        _context = context;
        _keyVaultService = keyVaultService;
        _bybitService = bybitService;
        _cmcService = cmcService;
    }

    public async Task<Response> Handle(GetReconciliationQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Include(a => a.CryptoAssets)
            .Where(a => a.Id == request.AccountId && a.UserId == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
            return new Response("Account not found", false, 404);

        var apiKey = await _keyVaultService.GetSecretAsync(
            SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-key"));
        var apiSecret = await _keyVaultService.GetSecretAsync(
            SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-secret"));

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            return new Response("Bybit credentials not configured for this account", false, 400);

        var bybitTotal = await _bybitService.GetTotalEquityAsync(apiKey, apiSecret);

        // App total = fiat balance + sum of (crypto holdings × market price)
        var appTotal = account.Balance;
        foreach (var asset in account.CryptoAssets)
        {
            if (asset.Balance <= 0) continue;
            try
            {
                var quote = await _cmcService.GetQuoteBySymbol(asset.Symbol.ToUpperInvariant());
                var price = quote?.Data?.FirstOrDefault().Value?.Quote?.USD?.Price ?? 0;
                appTotal += asset.Balance * price;
            }
            catch
            {
                // skip crypto prices that fail to load
            }
        }

        var drift = bybitTotal - appTotal;
        return new Response("ok", true, new ReconciliationDto(Math.Round(bybitTotal, 2), Math.Round(appTotal, 2), Math.Round(drift, 2)));
    }
}
