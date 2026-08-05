using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Commands;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetExchangeAccountDetailQuery(int UserId, int AccountId) : IRequest<Response>;

public record ExchangeAccountDetailDto(
    int AccountId,
    string AccountName,
    List<ExchangeConnectionDto> Connections);

public record ExchangeConnectionDto(
    string ExchangeName,
    string Status,
    DateTime? LastSyncAt,
    int ErrorCount,
    string? LastErrorMessage,
    bool HasApiKey,
    bool HasApiSecret,
    bool HasWebhookSecret);

public class GetExchangeAccountDetailQueryHandler : IRequestHandler<GetExchangeAccountDetailQuery, Response>
{
    private readonly DataContext _context;
    private readonly IKeyVaultService _keyVaultService;

    public GetExchangeAccountDetailQueryHandler(DataContext context, IKeyVaultService keyVaultService)
    {
        _context = context;
        _keyVaultService = keyVaultService;
    }

    public async Task<Response> Handle(GetExchangeAccountDetailQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Where(a => a.Id == request.AccountId && a.UserId == request.UserId)
            .Select(a => new { a.Id, a.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
            return new Response("Account not found", false, 404);

        var syncStatuses = await _context.SyncStatuses
            .Where(s => s.UserId == request.UserId && s.AccountId == request.AccountId)
            .ToListAsync(cancellationToken);

        // For each exchange with a SyncStatus, check credentials in Key Vault
        var connections = new List<ExchangeConnectionDto>();

        foreach (var status in syncStatuses)
        {
            var hasApiKey = !string.IsNullOrEmpty(
                await _keyVaultService.GetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-key")));
            var hasApiSecret = !string.IsNullOrEmpty(
                await _keyVaultService.GetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-secret")));
            var hasWebhookSecret = !string.IsNullOrEmpty(
                await _keyVaultService.GetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "webhook-secret")));

            connections.Add(new ExchangeConnectionDto(
                status.ExchangeName,
                status.Status,
                status.LastSyncAt,
                status.ErrorCount,
                status.LastErrorMessage,
                hasApiKey,
                hasApiSecret,
                hasWebhookSecret));
        }

        // If no sync status exists, still report the account as NotConfigured
        if (connections.Count == 0)
        {
            connections.Add(new ExchangeConnectionDto(
                "Bybit",
                "NotConfigured",
                null,
                0,
                null,
                false,
                false,
                false));
        }

        return new Response("ok", true, new ExchangeAccountDetailDto(
            account.Id,
            account.Name,
            connections));
    }
}
