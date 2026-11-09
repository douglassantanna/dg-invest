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
    private readonly IBybitAccountSyncService _accountSyncService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<SyncBybitOrders> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public SyncBybitOrders(
        IBybitAccountSyncService accountSyncService,
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<SyncBybitOrders> logger,
        IConfiguration configuration,
        IEmailService emailService)
    {
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
}
