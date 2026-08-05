using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Models;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record SaveBybitCredentialsCommand(
    int UserId,
    int AccountId,
    string ApiKey,
    string ApiSecret,
    string WebhookSecret,
    string? Name = null,
    string? ExternalId = null) : IRequest<Response>;

public class SaveBybitCredentialsCommandValidator : AbstractValidator<SaveBybitCredentialsCommand>
{
    public SaveBybitCredentialsCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(-1);
        RuleFor(x => x.ApiKey).MaximumLength(255);
        RuleFor(x => x.ApiSecret).MaximumLength(255);
        RuleFor(x => x.WebhookSecret).MaximumLength(255);
        When(x => x.AccountId == 0, () =>
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        });
    }
}

public class SaveBybitCredentialsCommandHandler : IRequestHandler<SaveBybitCredentialsCommand, Response>
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<SaveBybitCredentialsCommandHandler> _logger;

    public SaveBybitCredentialsCommandHandler(
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<SaveBybitCredentialsCommandHandler> logger)
    {
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(SaveBybitCredentialsCommand request, CancellationToken cancellationToken)
    {
        var validator = new SaveBybitCredentialsCommandValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("SaveBybitCredentials validation failed: {Errors}", errors);
            return new Response("Validation failed", false, errors);
        }

        if (request.AccountId == 0)
        {
            return await HandleCreateAndSave(request, cancellationToken);
        }

        var account = await _context.Accounts
            .Where(a => a.Id == request.AccountId && a.UserId == request.UserId && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            _logger.LogError("SaveBybitCredentials: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            return new Response("Account not found", false, 404);
        }

        return await SaveSecretsAsync(request.UserId, request.AccountId, request.ApiKey, request.ApiSecret, request.WebhookSecret, cancellationToken);
    }

    private async Task<Response> HandleCreateAndSave(SaveBybitCredentialsCommand request, CancellationToken cancellationToken)
    {
        var existingAccount = await _context.Accounts
            .Where(a => a.Name == request.Name && a.UserId == request.UserId && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAccount != null)
        {
            _logger.LogError("SaveBybitCredentials: account with name '{Name}' already exists for user {UserId}", request.Name, request.UserId);
            return new Response($"An account with the name '{request.Name}' already exists", false, 400);
        }

        var account = new Account(
            request.Name!,
            request.UserId,
            EAccountType.Exchange,
            "Bybit",
            string.IsNullOrEmpty(request.ExternalId) ? null : request.ExternalId);
        if (!string.IsNullOrEmpty(request.ExternalId))
        {
            account.SetExternalId(request.ExternalId);
        }

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SaveBybitCredentials: created account {AccountId} with name '{Name}' for user {UserId}", account.Id, request.Name, request.UserId);

        return await SaveSecretsAsync(request.UserId, account.Id, request.ApiKey, request.ApiSecret, request.WebhookSecret, cancellationToken);
    }

    private async Task<Response> SaveSecretsAsync(int userId, int accountId, string apiKey, string apiSecret, string webhookSecret, CancellationToken cancellationToken)
    {
        try
        {
            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(s => s.UserId == userId && s.AccountId == accountId && s.ExchangeName == "Bybit", cancellationToken);
            if (syncStatus == null)
            {
                syncStatus = new SyncStatus(userId, accountId, "Bybit");
                _context.SyncStatuses.Add(syncStatus);
            }
            syncStatus.MarkCredentialsSet();
            await _context.SaveChangesAsync(cancellationToken);

            await _keyVaultService.SetSecretAsync(BuildKey(userId, accountId, "api-key"), apiKey);
            await _keyVaultService.SetSecretAsync(BuildKey(userId, accountId, "api-secret"), apiSecret);
            await _keyVaultService.SetSecretAsync(BuildKey(userId, accountId, "webhook-secret"), webhookSecret);

            _logger.LogInformation("Bybit credentials saved for user {UserId}, account {AccountId}", userId, accountId);
            return new Response("Credentials saved successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Bybit credentials for user {UserId}, account {AccountId}", userId, accountId);
            return new Response("Failed to save credentials", false, 500);
        }
    }

    public static string BuildKey(int userId, int accountId, string suffix)
        => $"bybit-{userId}-{accountId}-{suffix}";
}
