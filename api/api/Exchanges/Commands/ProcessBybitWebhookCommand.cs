using api.AzureKeyVault;
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
        var webhookSecret = await _keyVaultService.GetSecretAsync(
            SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "webhook-secret"));

        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogWarning("ProcessBybitWebhook: webhook secret not configured for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Webhook secret not configured", false, 401);
        }

        if (!_bybitService.ValidateWebhookSignature(request.RawBody, request.Signature, request.Timestamp, webhookSecret))
        {
            _logger.LogWarning("ProcessBybitWebhook: invalid signature for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Invalid signature", false, 401);
        }

        if (!request.Payload.Topic.Equals("order", StringComparison.OrdinalIgnoreCase))
            return new Response("ok", true);

        var account = await _context.Accounts
            .Include(a => a.CryptoAssets)
                .ThenInclude(ca => ca.Transactions)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

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
