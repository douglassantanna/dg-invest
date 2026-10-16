using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Models;
using api.Shared;
using api.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Queries;

public record GetAccountSelectorOptionsQuery(int UserId) : IRequest<Response>;

public class GetAccountSelectorOptionsQueryHandler(DataContext context)
    : IRequestHandler<GetAccountSelectorOptionsQuery, Response>
{
    public async Task<Response> Handle(GetAccountSelectorOptionsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await context.Accounts
            .AsNoTracking()
            .Where(account => account.UserId == request.UserId
                && !account.IsDeleted)
            .OrderByDescending(account => account.IsSelected)
            .ThenBy(account => account.AccountType)
            .ThenBy(account => account.Name)
            .Select(account => new
            {
                account.Id,
                account.Name,
                account.Balance,
                account.IsSelected,
                account.AccountType,
                account.Exchange,
                account.Enabled
            })
            .ToListAsync(cancellationToken);

        var exchangeAccounts = accounts.Where(account => account.AccountType == EAccountType.Exchange).ToArray();
        var exchangeAccountIds = exchangeAccounts.Select(account => account.Id).ToArray();
        var syncStatuses = await context.SyncStatuses
            .AsNoTracking()
            .Where(status => status.UserId == request.UserId && exchangeAccountIds.Contains(status.AccountId))
            .Select(status => new
            {
                status.AccountId,
                status.ExchangeName,
                status.Status,
                status.IsEnabled,
                status.LastSyncAt,
                status.ErrorCount,
                status.LastErrorMessage
            })
            .ToListAsync(cancellationToken);
        var syncStatusByAccountId = syncStatuses
            .GroupBy(status => status.AccountId)
            .ToDictionary(group => group.Key, group => group.First());

        var exchangeNames = exchangeAccounts
            .Select(account => account.Exchange)
            .Where(exchange => !string.IsNullOrWhiteSpace(exchange))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var integrations = await context.ExchangeIntegrations
            .AsNoTracking()
            .Where(integration => integration.UserId == request.UserId && exchangeNames.Contains(integration.Exchange))
            .Select(integration => new
            {
                integration.Exchange,
                integration.Status,
                integration.Enabled,
                integration.LastSyncAt,
                integration.ConsecutiveTransportFailures,
                integration.LastErrorMessage
            })
            .ToListAsync(cancellationToken);
        var integrationByExchange = integrations.ToDictionary(integration => integration.Exchange, StringComparer.OrdinalIgnoreCase);

        var options = new List<AccountSelectorOptionDto>(accounts.Count);
        foreach (var account in accounts)
        {
            if (account.AccountType == EAccountType.Manual)
            {
                options.Add(new AccountSelectorOptionDto(
                    account.Id,
                    account.Name,
                    account.Balance,
                    account.IsSelected,
                    account.AccountType.ToString(),
                    null,
                    false,
                    null,
                    null,
                    0,
                    null));
                continue;
            }

            integrationByExchange.TryGetValue(account.Exchange ?? string.Empty, out var integration);

            syncStatusByAccountId.TryGetValue(account.Id, out var syncStatus);
            options.Add(new AccountSelectorOptionDto(
                account.Id,
                account.Name,
                account.Balance,
                account.IsSelected,
                account.AccountType.ToString(),
                account.Exchange,
                string.Equals(account.Exchange, "Bybit", StringComparison.OrdinalIgnoreCase),
                GetHealthStatus(account.Enabled, syncStatus?.Status, syncStatus?.IsEnabled, integration?.Enabled, integration?.Status),
                syncStatus?.LastSyncAt ?? integration?.LastSyncAt,
                syncStatus?.ErrorCount ?? integration?.ConsecutiveTransportFailures ?? 0,
                syncStatus?.LastErrorMessage ?? integration?.LastErrorMessage));
        }

        return new Response("ok", true, options);
    }

    private static string GetHealthStatus(
        bool accountEnabled,
        string? syncStatus,
        bool? isSyncEnabled,
        bool? integrationEnabled,
        string? integrationStatus)
    {
        if (integrationStatus is ExchangeIntegration.AutoPausedStatus or "Error"
            || string.Equals(syncStatus, "Error", StringComparison.OrdinalIgnoreCase))
            return "NeedsAttention";

        if (!accountEnabled || isSyncEnabled is false || integrationEnabled is false)
            return "Paused";

        return syncStatus is null ? "NotConfigured" : "Connected";
    }
}
