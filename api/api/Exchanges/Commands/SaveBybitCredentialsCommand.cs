using api.AzureKeyVault;
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
    string WebhookSecret) : IRequest<Response>;

public class SaveBybitCredentialsCommandValidator : AbstractValidator<SaveBybitCredentialsCommand>
{
    public SaveBybitCredentialsCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.ApiKey).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ApiSecret).NotEmpty().MaximumLength(255);
        RuleFor(x => x.WebhookSecret).NotEmpty().MaximumLength(255);
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

        var account = await _context.Accounts
            .Where(a => a.Id == request.AccountId && a.UserId == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            _logger.LogError("SaveBybitCredentials: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            return new Response("Account not found", false, 404);
        }

        try
        {
            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.AccountId == request.AccountId && s.ExchangeName == "Bybit", cancellationToken);
            if (syncStatus == null)
            {
                syncStatus = new SyncStatus(request.UserId, request.AccountId, "Bybit");
                _context.SyncStatuses.Add(syncStatus);
            }
            syncStatus.MarkCredentialsSet();
            await _context.SaveChangesAsync(cancellationToken);

            await _keyVaultService.SetSecretAsync(BuildKey(request.UserId, request.AccountId, "api-key"), request.ApiKey);
            await _keyVaultService.SetSecretAsync(BuildKey(request.UserId, request.AccountId, "api-secret"), request.ApiSecret);
            await _keyVaultService.SetSecretAsync(BuildKey(request.UserId, request.AccountId, "webhook-secret"), request.WebhookSecret);

            _logger.LogInformation("Bybit credentials saved for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Credentials saved successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Bybit credentials for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Failed to save credentials", false, 500);
        }
    }

    public static string BuildKey(int userId, int accountId, string suffix)
        => $"bybit-{userId}-{accountId}-{suffix}";
}
