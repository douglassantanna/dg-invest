using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record ProcessBybitWebhookCommand(
    int UserId,
    int AccountId,
    BybitWebhookPayload Payload,
    string RawBody,
    string Signature,
    string Timestamp) : IRequest<Response>;

public class ProcessBybitWebhookCommandHandler : IRequestHandler<ProcessBybitWebhookCommand, Response>
{
    private readonly IBybitService _bybitService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly IBybitOrderSyncService _orderSyncService;
    private readonly DataContext _context;
    private readonly ILogger<ProcessBybitWebhookCommandHandler> _logger;

    public ProcessBybitWebhookCommandHandler(
        IBybitService bybitService,
        IKeyVaultService keyVaultService,
        IBybitOrderSyncService orderSyncService,
        DataContext context,
        ILogger<ProcessBybitWebhookCommandHandler> logger)
    {
        _bybitService = bybitService;
        _keyVaultService = keyVaultService;
        _orderSyncService = orderSyncService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(ProcessBybitWebhookCommand request, CancellationToken cancellationToken)
    {
        var accountState = await _context.Accounts
            .Where(account => account.Id == request.AccountId && account.UserId == request.UserId)
            .Select(account => new
            {
                account.IsDeleted,
                account.AccountType,
                account.Exchange
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (accountState is null || accountState.IsDeleted || accountState.AccountType != EAccountType.Exchange || accountState.Exchange != "Bybit")
        {
            _logger.LogInformation("ProcessBybitWebhook: inactive Bybit account {AccountId} for user {UserId}", request.AccountId, request.UserId);
            return new Response("ok", true);
        }

        var integrationState = await _context.ExchangeIntegrations
            .Where(integration => integration.UserId == request.UserId && integration.Exchange == "Bybit")
            .Select(integration => new { integration.Enabled, integration.ActiveCredentialSetId })
            .SingleOrDefaultAsync(cancellationToken);
        if (integrationState is not null && (!integrationState.Enabled || integrationState.ActiveCredentialSetId == null))
        {
            _logger.LogInformation("ProcessBybitWebhook: integration is disconnected for user {UserId}", request.UserId);
            return new Response("ok", true);
        }

        var syncStatus = await _context.SyncStatuses.FirstOrDefaultAsync(
            status => status.UserId == request.UserId
                      && status.AccountId == request.AccountId
                      && status.ExchangeName == "Bybit",
            cancellationToken);
        if (syncStatus is null || !syncStatus.IsEnabled || syncStatus.ActiveCredentialSetId == null)
        {
            _logger.LogInformation("ProcessBybitWebhook: sync is disabled for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("ok", true);
        }

        var webhookSecret = await BybitCredentialReader.ReadAsync(_context, _keyVaultService, request.UserId, request.AccountId, "webhook-secret", cancellationToken);

        if (webhookSecret.IsUnavailable)
        {
            _logger.LogError("ProcessBybitWebhook: Key Vault unavailable for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);
        }

        if (string.IsNullOrEmpty(webhookSecret.Value))
        {
            _logger.LogWarning("ProcessBybitWebhook: webhook secret not configured for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Webhook secret not configured", false, 401);
        }

        if (!_bybitService.ValidateWebhookSignature(request.RawBody, request.Signature, request.Timestamp, webhookSecret.Value))
        {
            _logger.LogWarning("ProcessBybitWebhook: invalid signature for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Invalid signature", false, 401);
        }

        if (!request.Payload.Topic.Equals("order", StringComparison.OrdinalIgnoreCase))
            return new Response("ok", true);

        var account = await _context.Accounts
            .Include(a => a.CryptoAssets)
                .ThenInclude(ca => ca.Transactions)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId && !a.IsDeleted, cancellationToken);

        if (account == null)
        {
            _logger.LogError("ProcessBybitWebhook: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            await _orderSyncService.MarkSyncStatusErrorAsync(request.UserId, request.AccountId, "Account not found", cancellationToken);
            return new Response("Account not found", false, 404);
        }

        var filledOrders = request.Payload.Data.Where(o => o.OrderStatus == "Filled").ToList();

        foreach (var order in filledOrders)
        {
            await _orderSyncService.ProcessOrderAsync(order, account, request.UserId, "Webhook", cancellationToken);
        }

        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);

        var lastOrderId = filledOrders.LastOrDefault()?.OrderId;
        await _orderSyncService.UpsertSyncStatusAsync(request.UserId, request.AccountId, lastOrderId, cancellationToken);

        return new Response("ok", true);
    }
}
