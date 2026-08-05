using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
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

        if (account.Name.Equals("main", StringComparison.OrdinalIgnoreCase))
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
            await _keyVaultService.SetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-key"), string.Empty);
            await _keyVaultService.SetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-secret"), string.Empty);
            await _keyVaultService.SetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "webhook-secret"), string.Empty);

            account.SoftDelete();

            await _context.SaveChangesAsync(cancellationToken);

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
