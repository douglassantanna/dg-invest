using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record DeleteCredentialsCommand(int UserId, int AccountId) : IRequest<Response>;

public class DeleteCredentialsCommandHandler : IRequestHandler<DeleteCredentialsCommand, Response>
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<DeleteCredentialsCommandHandler> _logger;

    public DeleteCredentialsCommandHandler(
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<DeleteCredentialsCommandHandler> logger)
    {
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(DeleteCredentialsCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Where(a => a.Id == request.AccountId && a.UserId == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            _logger.LogError("DeleteCredentials: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            return new Response("Account not found", false, 404);
        }

        if (account.AccountType == EAccountType.Manual && account.Name.Equals("main", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("DeleteCredentials: main account {AccountId} cannot be deleted for user {UserId}", request.AccountId, request.UserId);
            return new Response("The main account cannot be deleted", false, 400);
        }

        if (account.AccountType != EAccountType.Exchange || !string.Equals(account.Exchange, "Bybit", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("DeleteCredentials: account {AccountId} is not a Bybit exchange account", request.AccountId);
            return new Response("Only Bybit exchange accounts can be deleted", false, 400);
        }

        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // Keep legacy callers from reading old fixed-name credentials during the migration.
                await _keyVaultService.SetSecretAsync(
                    SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-key"), string.Empty);
                await _keyVaultService.SetSecretAsync(
                    SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-secret"), string.Empty);
                await _keyVaultService.SetSecretAsync(
                    SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "webhook-secret"), string.Empty);

                await using var transaction = _context.Database.IsRelational()
                    ? await _context.Database.BeginTransactionAsync(cancellationToken)
                    : null;

                var status = await _context.SyncStatuses.SingleOrDefaultAsync(x =>
                    x.UserId == request.UserId && x.AccountId == request.AccountId && x.ExchangeName == "Bybit", cancellationToken);
                var activeSetId = status?.ActiveCredentialSetId;
                if (status != null) status.DeactivateCredentialSet();

                var operations = await _context.CredentialUpdateOperations.Where(x =>
                    x.UserId == request.UserId && x.AccountId == request.AccountId && x.Exchange == "Bybit" &&
                    (x.State == "Pending" || x.State == "VaultWritten" || x.State == "RecoveryRequired" || x.NewCredentialSetId == activeSetId))
                    .ToListAsync(cancellationToken);
                foreach (var operation in operations)
                {
                    if (operation.NewCredentialSetId == activeSetId) operation.MarkRetired();
                    else operation.MarkSuperseded();
                }

                account.SoftDelete();
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
            });

            _logger.LogInformation("Bybit account {AccountId} soft-deleted for user {UserId}", request.AccountId, request.UserId);
            return new Response("Subaccount removed", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Bybit credentials for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Failed to delete credentials", false, 500);
        }
    }
}
