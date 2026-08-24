using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record DisconnectBybitIntegrationCommand(int UserId) : IRequest<Response>;

public class DisconnectBybitIntegrationCommandHandler : IRequestHandler<DisconnectBybitIntegrationCommand, Response>
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<DisconnectBybitIntegrationCommandHandler> _logger;

    public DisconnectBybitIntegrationCommandHandler(
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<DisconnectBybitIntegrationCommandHandler> logger)
    {
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(DisconnectBybitIntegrationCommand request, CancellationToken cancellationToken)
    {
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(x => x.UserId == request.UserId && x.Exchange == "Bybit", cancellationToken);
        var accountIds = await _context.Accounts
            .Where(account => account.UserId == request.UserId
                              && !account.IsDeleted
                              && account.AccountType == EAccountType.Exchange
                              && account.Exchange == "Bybit")
            .Select(account => account.Id)
            .ToListAsync(cancellationToken);

        try
        {
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            if (integration != null)
            {
                integration.DeactivateCredentialSet();
                integration.MarkDisconnected();
            }

            var statuses = await _context.SyncStatuses
                .Where(status => status.UserId == request.UserId && status.ExchangeName == "Bybit")
                .ToListAsync(cancellationToken);
            foreach (var status in statuses)
            {
                status.DeactivateCredentialSet();
                status.Disable();
            }

            var activeOrIncompleteOperations = await _context.CredentialUpdateOperations
                .Where(operation => operation.UserId == request.UserId
                                    && operation.Exchange == "Bybit"
                                    && (operation.State == "Pending"
                                        || operation.State == "VaultWritten"
                                        || operation.State == "RecoveryRequired"
                                        || operation.State == "Active"))
                .ToListAsync(cancellationToken);
            foreach (var operation in activeOrIncompleteOperations)
            {
                if (operation.State == "Active") operation.MarkRetired();
                else operation.MarkSuperseded();
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Bybit integration for user {UserId}", request.UserId);
            return new Response("Failed to disconnect Bybit integration", false, 500);
        }

        try
        {
            await BlankLegacySecretsAsync(request.UserId, accountIds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bybit integration disconnected for user {UserId}, but legacy secret cleanup failed", request.UserId);
        }

        return new Response("Bybit integration disconnected", true);
    }

    private async Task BlankLegacySecretsAsync(int userId, IReadOnlyCollection<int> accountIds)
    {
        await _keyVaultService.SetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-key"), string.Empty);
        await _keyVaultService.SetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-secret"), string.Empty);

        foreach (var accountId in accountIds)
        {
            await _keyVaultService.SetSecretAsync(BybitCredentialKeys.LegacyAccountKey(userId, accountId, "api-key"), string.Empty);
            await _keyVaultService.SetSecretAsync(BybitCredentialKeys.LegacyAccountKey(userId, accountId, "api-secret"), string.Empty);
            await _keyVaultService.SetSecretAsync(BybitCredentialKeys.LegacyAccountKey(userId, accountId, "webhook-secret"), string.Empty);
        }
    }
}
