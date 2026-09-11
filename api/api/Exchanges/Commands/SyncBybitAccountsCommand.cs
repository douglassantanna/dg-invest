using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record SyncBybitAccountsCommand(int UserId) : IRequest<Response>;

public class SyncBybitAccountsCommandHandler : IRequestHandler<SyncBybitAccountsCommand, Response>
{
    private readonly IBybitService _bybitService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly IBybitOrderSyncService _orderSyncService;
    private readonly ILogger<SyncBybitAccountsCommandHandler> _logger;

    public SyncBybitAccountsCommandHandler(
        IBybitService bybitService,
        IKeyVaultService keyVaultService,
        DataContext context,
        IBybitOrderSyncService orderSyncService,
        ILogger<SyncBybitAccountsCommandHandler> logger)
    {
        _bybitService = bybitService;
        _keyVaultService = keyVaultService;
        _context = context;
        _orderSyncService = orderSyncService;
        _logger = logger;
    }

    public async Task<Response> Handle(SyncBybitAccountsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                _logger.LogError("SyncBybitAccounts: user {UserId} not found", request.UserId);
                return new Response("User not found", false, 404);
            }

            var integration = await _context.ExchangeIntegrations
                .SingleOrDefaultAsync(x => x.UserId == request.UserId && x.Exchange == "Bybit", cancellationToken);
            if (integration == null)
            {
                return new Response("Bybit integration credentials not found. Please save your API key and secret first.", false, 400);
            }

            if (!integration.Enabled)
                return new Response("Bybit integration is disconnected. Save integration credentials to reconnect.", false, 400);

            var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, request.UserId, null, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, request.UserId, null, "api-secret", cancellationToken);

            if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
                return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);

            if (string.IsNullOrEmpty(apiKey.Value) || string.IsNullOrEmpty(apiSecret.Value))
            {
                _logger.LogError("SyncBybitAccounts: Bybit credentials not configured for user {UserId}", request.UserId);
                return new Response("Bybit credentials not found. Please save your API key and secret first.", false, 400);
            }

            var region = BybitEndpoints.Parse(integration.Region);
            List<BybitSubMember> subMembers;
            try
            {
                subMembers = await _bybitService.GetSubAccountsAsync(apiKey.Value!, apiSecret.Value!, region);
            }
            catch (BybitApiException ex)
            {
                _logger.LogWarning(ex, "SyncBybitAccounts: Bybit rejected discovery request for user {UserId}", request.UserId);
                return new Response($"Bybit rejected the integration credentials: {ex.RetCode} - {ex.RetMsg}", false, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncBybitAccounts: failed to fetch sub-accounts from Bybit for user {UserId}", request.UserId);
                return new Response("Failed to fetch sub-accounts from Bybit", false, 500);
            }

            var existingBybitAccounts = await _context.Accounts
                .Where(a => a.UserId == request.UserId && !a.IsDeleted
                         && a.AccountType == EAccountType.Exchange && a.Exchange == "Bybit" && a.ExternalId != null
                         && a.Id != integration.MasterAccountId)
                .ToListAsync(cancellationToken);

            var existingByUid = existingBybitAccounts.ToDictionary(a => a.ExternalId!, a => a);
            var currentUids = new HashSet<string>(subMembers.Select(m => m.Uid), StringComparer.Ordinal);

            int created = 0;
            int matched = 0;
            int disabled = 0;

            var masterAccount = integration.MasterAccountId is { } masterAccountId
                ? await _context.Accounts.SingleOrDefaultAsync(a => a.Id == masterAccountId && !a.IsDeleted, cancellationToken)
                : null;
            if (masterAccount is not null)
                await PopulateInitialCashBalanceAsync(masterAccount, apiKey.Value!, apiSecret.Value!, region, "FUND", null, cancellationToken);
            else
            {
                var legacyMainAccount = await _context.Accounts
                    .SingleOrDefaultAsync(a => a.UserId == request.UserId && a.AccountType == EAccountType.Manual && a.Name == "main", cancellationToken);
                if (legacyMainAccount is not null)
                    await PopulateInitialCashBalanceAsync(legacyMainAccount, apiKey.Value!, apiSecret.Value!, region, "FUND", null, cancellationToken);
            }

            foreach (var member in subMembers)
            {
                if (existingByUid.TryGetValue(member.Uid, out var mappedAccount))
                {
                    if (!mappedAccount.Enabled)
                    {
                        mappedAccount.Enable();
                        _logger.LogInformation("SyncBybitAccounts: UID {Uid} re-enabled account '{Name}' for current connection",
                            member.Uid, mappedAccount.Name);
                    }

                    matched++;
                    _logger.LogInformation("SyncBybitAccounts: UID {Uid} already mapped to account '{Name}'",
                        member.Uid, mappedAccount.Name);
                    await PopulateInitialCashBalanceAsync(mappedAccount, apiKey.Value!, apiSecret.Value!, region, "UNIFIED", member.Uid, cancellationToken);
                    continue;
                }

                var tag = string.IsNullOrWhiteSpace(member.Remark)
                    ? member.Username.Trim()
                    : member.Remark.Trim();

                var newAccount = new Account(tag, request.UserId, EAccountType.Exchange, "Bybit", member.Uid);
                _context.Accounts.Add(newAccount);
                await PopulateInitialCashBalanceAsync(newAccount, apiKey.Value!, apiSecret.Value!, region, "UNIFIED", member.Uid, cancellationToken);
                created++;
                _logger.LogInformation("SyncBybitAccounts: created account '{Name}' (Bybit UID: {Uid}) for user {UserId}",
                    tag, member.Uid, request.UserId);
            }

            foreach (var existing in existingBybitAccounts)
            {
                if (existing.ExternalId != null && existing.Enabled && !currentUids.Contains(existing.ExternalId))
                {
                    existing.Disable();
                    disabled++;
                    _logger.LogInformation("SyncBybitAccounts: UID {Uid} not returned by current API key; disabling stale account '{Name}'",
                        existing.ExternalId, existing.Name);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new Response($"Sync complete. {matched} matched, {created} created, {disabled} disabled.", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncBybitAccounts: unexpected error for user {UserId}", request.UserId);
            return new Response("An unexpected error occurred during sync", false, 500);
        }


    }

    private async Task PopulateInitialCashBalanceAsync(Account account, string apiKey, string apiSecret, BybitRegion region, string accountType, string? memberId, CancellationToken cancellationToken)
    {
        if (account.Balance != 0)
            return;

        try
        {
            var balance = 0m;
            foreach (var coin in BybitCashBalance.CashCoins)
            {
                var coinBalance = await _bybitService.GetAccountCoinBalanceAsync(apiKey, apiSecret, region, accountType, coin, memberId);
                balance += BybitCashBalance.FromAccountCoinBalance(coinBalance);
            }
            await _orderSyncService.ProcessOpeningBalanceAsync(account, account.UserId, balance, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SyncBybitAccounts: failed to fetch {AccountType} cash balance for account {AccountId}", accountType, account.Id);
        }
    }
}
