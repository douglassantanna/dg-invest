using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record ProcessBybitOrdersCommand(
    int UserId,
    int AccountId,
    List<BybitOrderData> Orders) : IRequest<Response>;

public class ProcessBybitOrdersCommandHandler : IRequestHandler<ProcessBybitOrdersCommand, Response>
{
    private readonly IBybitOrderSyncService _orderSyncService;
    private readonly DataContext _context;
    private readonly ILogger<ProcessBybitOrdersCommandHandler> _logger;

    public ProcessBybitOrdersCommandHandler(
        IBybitOrderSyncService orderSyncService,
        DataContext context,
        ILogger<ProcessBybitOrdersCommandHandler> logger)
    {
        _orderSyncService = orderSyncService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(ProcessBybitOrdersCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Include(a => a.CryptoAssets)
                .ThenInclude(ca => ca.Transactions)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

        if (account == null)
        {
            _logger.LogError("ProcessBybitOrders: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            await _orderSyncService.MarkSyncStatusErrorAsync(request.UserId, request.AccountId, "Account not found", cancellationToken);
            return new Response("Account not found", false, 404);
        }

        var filledOrders = request.Orders.Where(o => o.OrderStatus == "Filled").ToList();
        if (filledOrders.Count == 0)
            return new Response("No filled orders to process", true);

        foreach (var order in filledOrders)
        {
            await _orderSyncService.ProcessOrderAsync(order, account, request.UserId, "REST", cancellationToken);
        }

        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);

        var lastOrderId = filledOrders.LastOrDefault()?.OrderId;
        await _orderSyncService.UpsertSyncStatusAsync(request.UserId, request.AccountId, lastOrderId, cancellationToken);

        return new Response($"Processed {filledOrders.Count} order(s)", true);
    }
}
