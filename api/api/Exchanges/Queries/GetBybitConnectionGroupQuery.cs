using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Models;
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
    List<BybitSubaccountRowDto> Subaccounts,
    string IntegrationStatus = "NotSetup",
    bool IntegrationEnabled = false,
    int ConsecutiveTransportFailures = 0,
    string? LastErrorCode = null,
    string? LastErrorMessage = null,
    string? LastErrorEndpoint = null,
    int? LastErrorAccountId = null,
    DateTime? AutoPausedAt = null);

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
    bool IsEnabled,
    bool IsMaster = false)
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
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(x => x.UserId == request.UserId && x.Exchange == "Bybit", cancellationToken);
        if (integration != null && !integration.Enabled && integration.Status != ExchangeIntegration.AutoPausedStatus)
            return new Response("ok", true, EmptyGroup(integration));

        var accounts = await _context.Accounts
            .Where(a => a.UserId == request.UserId && !a.IsDeleted && a.Enabled
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
            var syncStatus = syncStatuses
                .FirstOrDefault(s => s.AccountId == account.Id);

            var hasApiKey = false;
            var hasApiSecret = false;
            var hasWebhookSecret = false;
            string? apiKeyValue = null;
            if (integration?.Status == ExchangeIntegration.AutoPausedStatus)
            {
                // A paused connection must remain inspectable without causing more Key Vault reads.
                var credentialsWereConfigured = syncStatus?.BybitCredentialsSetAt is not null
                    || integration?.MasterAccountId == account.Id;
                hasApiKey = credentialsWereConfigured;
                hasApiSecret = credentialsWereConfigured;
            }
            else
            {
                var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, request.UserId, account.Id, "api-key", cancellationToken);
                var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, request.UserId, account.Id, "api-secret", cancellationToken);
                var webhookSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, request.UserId, account.Id, "webhook-secret", cancellationToken);

                if (apiKey.IsUnavailable || apiSecret.IsUnavailable || webhookSecret.IsUnavailable)
                    return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);

                hasApiKey = apiKey.IsFound && !string.IsNullOrEmpty(apiKey.Value);
                hasApiSecret = apiSecret.IsFound && !string.IsNullOrEmpty(apiSecret.Value);
                hasWebhookSecret = webhookSecret.IsFound && !string.IsNullOrEmpty(webhookSecret.Value);
                apiKeyValue = apiKey.Value;
            }

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

            var maskedApiKey = hasApiKey && apiKeyValue is { Length: > 4 }
                ? "...." + apiKeyValue[^4..]
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
                IsEnabled: syncStatus?.IsEnabled ?? false,
                IsMaster: account.Id == integration?.MasterAccountId));
        }

        var group = new BybitConnectionGroupDto(
            Id: "bybit-main",
            Name: "Main account (Bybit login)",
            SubaccountCount: rows.Count(row => !row.IsMaster),
            MaxSubaccounts: maxSubaccounts,
            Subaccounts: rows,
            IntegrationStatus: integration?.Status ?? "NotSetup",
            IntegrationEnabled: integration?.Enabled ?? false,
            ConsecutiveTransportFailures: integration?.ConsecutiveTransportFailures ?? 0,
            LastErrorCode: integration?.LastErrorCode,
            LastErrorMessage: integration?.LastErrorMessage,
            LastErrorEndpoint: integration?.LastErrorEndpoint,
            LastErrorAccountId: integration?.LastErrorAccountId,
            AutoPausedAt: integration?.AutoPausedAt);

        return new Response("ok", true, new List<BybitConnectionGroupDto> { group });
    }

    private static List<BybitConnectionGroupDto> EmptyGroup(ExchangeIntegration? integration = null) =>
    [
        new(
            Id: "bybit-main",
            Name: "Main account (Bybit login)",
            SubaccountCount: 0,
            MaxSubaccounts: 10,
            Subaccounts: [],
            IntegrationStatus: integration?.Status ?? "NotSetup",
            IntegrationEnabled: integration?.Enabled ?? false,
            ConsecutiveTransportFailures: integration?.ConsecutiveTransportFailures ?? 0,
            LastErrorCode: integration?.LastErrorCode,
            LastErrorMessage: integration?.LastErrorMessage,
            LastErrorEndpoint: integration?.LastErrorEndpoint,
            LastErrorAccountId: integration?.LastErrorAccountId,
            AutoPausedAt: integration?.AutoPausedAt)
    ];

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
