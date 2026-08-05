using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Commands;
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
    bool IsEnabled);

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
            .Where(a => a.UserId == request.UserId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var syncStatuses = await _context.SyncStatuses
            .Where(s => s.UserId == request.UserId && s.ExchangeName == "Bybit")
            .ToListAsync(cancellationToken);

        var rows = new List<BybitSubaccountRowDto>();
        const int maxSubaccounts = 10;

        foreach (var account in accounts)
        {
            var apiKey = await _keyVaultService.GetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, account.Id, "api-key"));
            var apiSecret = await _keyVaultService.GetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, account.Id, "api-secret"));
            var webhookSecret = await _keyVaultService.GetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, account.Id, "webhook-secret"));

            var hasApiKey = !string.IsNullOrEmpty(apiKey);
            var hasApiSecret = !string.IsNullOrEmpty(apiSecret);
            var hasWebhookSecret = !string.IsNullOrEmpty(webhookSecret);

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

            var maskedApiKey = hasApiKey && apiKey!.Length > 4
                ? "...." + apiKey[^4..]
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
