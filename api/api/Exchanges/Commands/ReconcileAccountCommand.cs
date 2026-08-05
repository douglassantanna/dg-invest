using api.AzureKeyVault;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record ReconcileAccountCommand(int UserId, int AccountId) : IRequest<Response>;

public class ReconcileAccountCommandHandler : IRequestHandler<ReconcileAccountCommand, Response>
{
    private readonly DataContext _context;
    private readonly IKeyVaultService _keyVaultService;
    private readonly IBybitService _bybitService;
    private readonly ICoinMarketCapService _cmcService;
    private readonly ITransactionService _transactionService;

    public ReconcileAccountCommandHandler(
        DataContext context,
        IKeyVaultService keyVaultService,
        IBybitService bybitService,
        ICoinMarketCapService cmcService,
        ITransactionService transactionService)
    {
        _context = context;
        _keyVaultService = keyVaultService;
        _bybitService = bybitService;
        _cmcService = cmcService;
        _transactionService = transactionService;
    }

    public async Task<Response> Handle(ReconcileAccountCommand request, CancellationToken cancellationToken)
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
            catch { }
        }

        var drift = bybitTotal - appTotal;
        if (Math.Abs(drift) < 5)
            return new Response($"Drift is only ${Math.Abs(drift):F2} — below $5 threshold. No adjustment needed.", true);

        var adjustmentTx = new AccountTransaction(
            date: DateTime.UtcNow,
            transactionType: drift > 0 ? EAccountTransactionType.DepositFiat : EAccountTransactionType.WithdrawToBank,
            amount: Math.Abs(drift),
            notes: $"Reconciliation — Bybit balance ${bybitTotal:F2}, app was ${appTotal:F2}");

        var result = _transactionService.ExecuteTransaction(account, adjustmentTx);
        if (!result.IsSuccess)
            return new Response($"Failed to create adjustment: {result.Message}", false, 500);

        await _context.SaveChangesAsync(cancellationToken);

        return new Response($"Reconciled — adjusted by ${drift:F2}. New app total: ${bybitTotal:F2}", true);
    }
}
