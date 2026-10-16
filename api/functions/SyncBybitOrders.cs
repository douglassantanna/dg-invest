using api.AzureKeyVault;
using api.Exchanges.Services;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Services.Contracts;
using api.Users.Models;
using api.Exchanges.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions;

public class SyncBybitOrders
{
    private readonly IBybitService _bybitService;
    private readonly IBybitOrderSyncService _orderSyncService;
    private readonly IBybitAccountSyncService _accountSyncService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<SyncBybitOrders> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public SyncBybitOrders(
        IBybitService bybitService,
        IBybitOrderSyncService orderSyncService,
        IBybitAccountSyncService accountSyncService,
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<SyncBybitOrders> logger,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _bybitService = bybitService;
        _orderSyncService = orderSyncService;
        _accountSyncService = accountSyncService;
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
    }

    [Function("SyncBybitOrders")]
    public async Task Run([TimerTrigger("*/30 * * * * *")] TimerInfo timer, FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;

        var syncEnabled = _configuration.GetValue<bool>("BybitSync:Enabled");
        if (!syncEnabled)
        {
            _logger.LogDebug("SyncBybitOrders: feature flag BybitSync:Enabled is false, skipping");
            return;
        }

        try
        {
            await SendPendingPauseNotificationsAsync(cancellationToken);

            var accounts = await _context.Accounts
                .Include(a => a.CryptoAssets)
                    .ThenInclude(ca => ca.Transactions)
                .Where(a => a.ExternalId != null
                            && a.Enabled
                            && !a.IsDeleted
                            && a.AccountType == EAccountType.Exchange
                            && a.Exchange == "Bybit"
                            && _context.ExchangeIntegrations
                                .Where(integration => integration.UserId == a.UserId && integration.Exchange == "Bybit")
                                .All(integration => integration.Enabled))
                .ToListAsync(cancellationToken);

            if (accounts.Count == 0)
                return;

            foreach (var accountsByUser in accounts.GroupBy(account => account.UserId))
            {
                var userAccounts = accountsByUser.ToList();
                BybitAccountSyncResult? classifiedFailure = null;
                BybitAccountSyncResult? otherFailure = null;
                int? failureAccountId = null;

                foreach (var account in userAccounts)
                {
                    var result = await _accountSyncService.SyncAsync(account, cancellationToken);
                    if (result.FailureKind is BybitApiFailureKind.Transport or BybitApiFailureKind.PermanentCredential)
                    {
                        classifiedFailure = result;
                        failureAccountId = account.Id;
                        break;
                    }

                    if (!result.IsSuccess && !result.IsSkipped)
                        otherFailure ??= result;
                }

                if (classifiedFailure is not null)
                {
                    await RecordIntegrationFailureAsync(userAccounts, classifiedFailure, failureAccountId, cancellationToken);
                    continue;
                }

                var transferResult = await SyncUniversalTransfersAsync(userAccounts, cancellationToken);
                if (transferResult.FailureKind is BybitApiFailureKind.Transport or BybitApiFailureKind.PermanentCredential)
                {
                    await RecordIntegrationFailureAsync(userAccounts, transferResult, null, cancellationToken);
                    continue;
                }

                if (!transferResult.IsSuccess && !transferResult.IsSkipped)
                    otherFailure ??= transferResult;

                if (otherFailure is not null)
                {
                    await RecordNonTransportFailureAsync(accountsByUser.Key, otherFailure, cancellationToken);
                    continue;
                }

                await RecordSuccessfulCycleAsync(accountsByUser.Key, cancellationToken);
            }

            await SendPendingPauseNotificationsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("SyncBybitOrders: cancellation requested");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncBybitOrders: unexpected error");
        }
    }

    private async Task RecordIntegrationFailureAsync(
        IReadOnlyCollection<Account> accounts,
        BybitAccountSyncResult failure,
        int? accountId,
        CancellationToken cancellationToken)
    {
        var userId = accounts.First().UserId;
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Exchange == "Bybit", cancellationToken);
        if (integration is null || !integration.Enabled)
            return;

        var now = DateTime.UtcNow;
        var paused = failure.FailureKind == BybitApiFailureKind.PermanentCredential
            ? integration.RecordPermanentCredentialFailure(
                failure.ErrorCode ?? "credential-rejected",
                failure.Message,
                failure.Endpoint ?? "Bybit API request",
                accountId,
                now)
            : integration.RecordTransportFailure(
                failure.ErrorCode ?? "transport",
                failure.Message,
                failure.Endpoint ?? "Bybit API request",
                accountId,
                now);

        await _context.SaveChangesAsync(cancellationToken);

        BybitCredentialReader.Invalidate(_keyVaultService, userId, null);
        foreach (var account in accounts)
            BybitCredentialReader.Invalidate(_keyVaultService, userId, account.Id);

        if (paused)
        {
            _logger.LogError(
                "Bybit integration automatically paused for user {UserId}, account {AccountId}, error code {ErrorCode}, endpoint {Endpoint}, consecutive transport failures {FailureCount}",
                userId,
                accountId,
                integration.LastErrorCode,
                integration.LastErrorEndpoint,
                integration.ConsecutiveTransportFailures);
        }
        else
        {
            _logger.LogDebug(
                "Bybit integration transport failure for user {UserId}, account {AccountId}, retry {FailureCount} of {Threshold}, endpoint {Endpoint}",
                userId,
                accountId,
                integration.ConsecutiveTransportFailures,
                ExchangeIntegration.TransportFailurePauseThreshold,
                integration.LastErrorEndpoint);
        }
    }

    private async Task RecordSuccessfulCycleAsync(int userId, CancellationToken cancellationToken)
    {
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Exchange == "Bybit" && item.Enabled, cancellationToken);
        if (integration is null)
            return;

        integration.MarkSyncCycleSucceeded(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordNonTransportFailureAsync(int userId, BybitAccountSyncResult failure, CancellationToken cancellationToken)
    {
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Exchange == "Bybit" && item.Enabled, cancellationToken);
        if (integration is null)
            return;

        integration.MarkNonTransportFailure(failure.Message, null, DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Bybit sync recorded a non-transport failure for user {UserId}: {Message}", userId, failure.Message);
    }

    private async Task SendPendingPauseNotificationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pending = await _context.ExchangeIntegrations
            .Where(item => item.Exchange == "Bybit"
                && item.Status == ExchangeIntegration.AutoPausedStatus
                && item.PauseNotificationSentAt == null
                && item.PauseNotificationAttempts < ExchangeIntegration.MaxPauseNotificationAttempts)
            .ToListAsync(cancellationToken);

        foreach (var integration in pending)
        {
            if (!integration.CanAttemptPauseNotification(now))
                continue;

            var user = await _context.Users.SingleOrDefaultAsync(candidate => candidate.Id == integration.UserId, cancellationToken);
            var sent = false;
            try
            {
                if (user is null)
                    throw new InvalidOperationException($"User {integration.UserId} no longer exists.");

                await _emailService.SendBybitIntegrationPausedAlertAsync(
                    new BybitIntegrationPausedAlert(
                        user.FullName,
                        user.Email,
                        integration.UserId,
                        integration.LastErrorAccountId,
                        integration.LastErrorCode,
                        integration.LastErrorMessage ?? "Bybit sync failed repeatedly.",
                        integration.LastErrorEndpoint,
                        integration.ConsecutiveTransportFailures,
                        integration.AutoPausedAt ?? now),
                    cancellationToken);
                sent = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Could not send Bybit auto-pause notification for user {UserId}, attempt {Attempt}",
                    integration.UserId, integration.PauseNotificationAttempts + 1);
            }

            integration.RecordPauseNotificationAttempt(now, sent);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<BybitAccountSyncResult> SyncUniversalTransfersAsync(IReadOnlyCollection<Account> exchangeAccounts, CancellationToken cancellationToken)
    {
        var userId = exchangeAccounts.First().UserId;
        var integration = await _context.ExchangeIntegrations
            .FirstOrDefaultAsync(i => i.UserId == userId && i.Exchange == "Bybit" && i.Enabled, cancellationToken);
        if (integration is null)
            return BybitAccountSyncResult.Succeeded("Bybit integration is disabled or not configured");

        var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, null, "api-key", cancellationToken);
        var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, null, "api-secret", cancellationToken);
        if (apiKey.IsUnavailable || apiSecret.IsUnavailable
            || string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
            return BybitAccountSyncResult.Succeeded("Integration-level Bybit credentials are not configured");

        var startTime = integration.LastSyncAt is { } lastSyncAt
            ? new DateTimeOffset(lastSyncAt, TimeSpan.Zero).ToUnixTimeMilliseconds()
            : (long?)null;
        var region = BybitEndpoints.Parse(integration.Region);
        List<BybitInternalTransferRow> universalTransfers;
        try
        {
            universalTransfers = await _bybitService.GetUniversalTransferHistoryAsync(
                apiKey.Value, apiSecret.Value, region, limit: 50, startTime: startTime, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var failure = BybitApiFailureClassifier.Classify(ex);
            return BybitAccountSyncResult.Failed(
                failure.Kind == BybitApiFailureKind.PermanentCredential
                    ? $"Bybit rejected integration credentials: {ex.Message}"
                    : $"Bybit universal transfer request failed: {ex.Message}",
                failure.Kind == BybitApiFailureKind.Transport ? 503 : 400,
                failure.Kind,
                failure.Code,
                failure.Endpoint);
        }

        var candidateAccounts = exchangeAccounts
            .Where(account => account.Enabled && !account.IsDeleted)
            .ToList();
        var readyAccountIds = new HashSet<int>();
        foreach (var account in candidateAccounts)
        {
            var accountApiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, account.Id, "api-key", cancellationToken);
            var accountApiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, account.Id, "api-secret", cancellationToken);
            if (accountApiKey.IsFound && !string.IsNullOrWhiteSpace(accountApiKey.Value)
                && accountApiSecret.IsFound && !string.IsNullOrWhiteSpace(accountApiSecret.Value))
                readyAccountIds.Add(account.Id);
        }
        var hasFailures = false;

        foreach (var transfer in universalTransfers)
        {
            var sourceAccount = FindTransferAccount(candidateAccounts, transfer.FromAccountType, transfer.FromMemberId);
            var destinationAccount = FindTransferAccount(candidateAccounts, transfer.ToAccountType, transfer.ToMemberId);
            if (sourceAccount is null || destinationAccount is null)
            {
                hasFailures = true;
                _logger.LogDebug(
                    "SyncBybitOrders: skipped universal transfer {TransferId} for user {UserId}; source UID {SourceMemberId} linked: {SourceLinked} (account {SourceAccountId}), destination UID {DestinationMemberId} linked: {DestinationLinked} (account {DestinationAccountId})",
                    transfer.TransferId,
                    userId,
                    transfer.FromMemberId,
                    sourceAccount is not null,
                    sourceAccount?.Id,
                    transfer.ToMemberId,
                    destinationAccount is not null,
                    destinationAccount?.Id);
                continue;
            }

            if (!readyAccountIds.Contains(sourceAccount.Id) || !readyAccountIds.Contains(destinationAccount.Id))
            {
                hasFailures = true;
                _logger.LogDebug("SyncBybitOrders: skipped universal transfer {TransferId}; source account {SourceAccountId} or destination account {DestinationAccountId} is not credential-ready",
                    transfer.TransferId, sourceAccount.Id, destinationAccount.Id);
                continue;
            }

            if (!await _orderSyncService.ProcessInternalTransferAsync(transfer, sourceAccount, userId, cancellationToken)
                || !await _orderSyncService.ProcessInternalTransferAsync(transfer, destinationAccount, userId, cancellationToken))
                hasFailures = true;
        }

        if (hasFailures)
            return BybitAccountSyncResult.Failed("One or more universal transfers could not be linked or processed.");

        return BybitAccountSyncResult.Succeeded($"Universal transfers synchronized for user {userId}");
    }

    private static Account? FindTransferAccount(IEnumerable<Account> accounts, string accountType, string memberId) =>
        accounts.FirstOrDefault(account => account.AccountType == EAccountType.Exchange
            && (accountType.Equals("FUND", StringComparison.OrdinalIgnoreCase)
                || accountType.Equals("UNIFIED", StringComparison.OrdinalIgnoreCase))
            && string.Equals(account.ExternalId, memberId, StringComparison.Ordinal));
}
