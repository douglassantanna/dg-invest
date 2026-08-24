using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetBybitConnectionGroupQuery(int UserId) : IRequest<Response>;

public record BybitConnectionGroupDto(
    string Id,
    string Name,
    int SubaccountCount,
    int MaxSubaccounts,
    List<BybitSubaccountRowDto> Subaccounts);

public record BybitSubaccountRowDto(
    int AccountId,
    string Name,
    string? ExternalId,
    string Status,
    bool HasApiKey,
    bool HasApiSecret,
    bool HasWebhookSecret,
    string? MaskedApiKey,
    string WebhookUrl,
    string? LastVerifiedAt,
    bool IsEnabled)
{
    public string? BybitUid => ExternalId;
}

public class GetBybitConnectionGroupQueryHandler : IRequestHandler<GetBybitConnectionGroupQuery, Response>
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;

    public GetBybitConnectionGroupQueryHandler(IKeyVaultService keyVaultService, DataContext context)
    {
        _keyVaultService = keyVaultService;
        _context = context;
    }

    public async Task<Response> Handle(GetBybitConnectionGroupQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == request.UserId && !a.IsDeleted
                     && a.AccountType == api.Cryptos.Models.EAccountType.Exchange && a.Exchange == "Bybit")
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var syncStatuses = await _context.SyncStatuses
            .Where(s => s.UserId == request.UserId && s.ExchangeName == "Bybit")
            .ToListAsync(cancellationToken);

        var rows = new List<BybitSubaccountRowDto>();
        const int maxSubaccounts = 10;

        foreach (var account in accounts)
        {
            var apiKey = await BybitCredentialReader.ReadAsync(_context, _keyVaultService, request.UserId, account.Id, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_context, _keyVaultService, request.UserId, account.Id, "api-secret", cancellationToken);
            var webhookSecret = await BybitCredentialReader.ReadAsync(_context, _keyVaultService, request.UserId, account.Id, "webhook-secret", cancellationToken);

            if (apiKey.IsUnavailable || apiSecret.IsUnavailable || webhookSecret.IsUnavailable)
                return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);

            var hasApiKey = apiKey.IsFound && !string.IsNullOrEmpty(apiKey.Value);
            var hasApiSecret = apiSecret.IsFound && !string.IsNullOrEmpty(apiSecret.Value);
            var hasWebhookSecret = webhookSecret.IsFound && !string.IsNullOrEmpty(webhookSecret.Value);

            var syncStatus = syncStatuses
                .FirstOrDefault(s => s.AccountId == account.Id);

            var hasAnyCredentials = hasApiKey || hasApiSecret || hasWebhookSecret;

            string status;
            if (!hasAnyCredentials)
                status = "pending";
            else if (syncStatus == null)
                status = "pending";
            else if (!syncStatus.IsEnabled)
                status = "paused";
            else if (syncStatus.Status == "Error")
                status = "err";
            else if (hasApiKey && hasApiSecret)
                status = "ok";
            else
                status = "pending";

            var maskedApiKey = hasApiKey && apiKey.Value!.Length > 4
                ? "...." + apiKey.Value[^4..]
                : null;

            var webhookUrl = hasWebhookSecret
                ? $"/api/tradewebhook/bybit/{request.UserId}/{account.Id}"
                : string.Empty;

            var lastVerifiedAt = syncStatus?.LastVerifiedAt is { } dt
                ? FormatRelativeTime(dt)
                : null;

            rows.Add(new BybitSubaccountRowDto(
                AccountId: account.Id,
                Name: account.Name,
                ExternalId: account.ExternalId,
                Status: status,
                HasApiKey: hasApiKey,
                HasApiSecret: hasApiSecret,
                HasWebhookSecret: hasWebhookSecret,
                MaskedApiKey: maskedApiKey,
                WebhookUrl: webhookUrl,
                LastVerifiedAt: lastVerifiedAt,
                IsEnabled: syncStatus?.IsEnabled ?? false));
        }

        var group = new BybitConnectionGroupDto(
            Id: "bybit-main",
            Name: "Main account (Bybit login)",
            SubaccountCount: rows.Count,
            MaxSubaccounts: maxSubaccounts,
            Subaccounts: rows);

        return new Response("ok", true, new List<BybitConnectionGroupDto> { group });
    }

    private static string FormatRelativeTime(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
        return utc.ToString("MMM dd, HH:mm");
    }
}
