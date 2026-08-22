using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Services;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.Exchanges.Commands;
public record SaveBybitCredentialsCommand(int UserId, int AccountId, string ApiKey, string ApiSecret, string WebhookSecret, string? Name = null, string? ExternalId = null) : IRequest<Response>;
public class SaveBybitCredentialsCommandValidator : AbstractValidator<SaveBybitCredentialsCommand>
{
    public SaveBybitCredentialsCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0); RuleFor(x => x.AccountId).GreaterThan(-1); RuleFor(x => x.ApiKey).MaximumLength(255); RuleFor(x => x.ApiSecret).MaximumLength(255); RuleFor(x => x.WebhookSecret).MaximumLength(255);
        When(x => x.AccountId == 0, () => { RuleFor(x => x.Name).NotEmpty().MaximumLength(255); RuleFor(x => x.ApiKey).NotEmpty(); RuleFor(x => x.ApiSecret).NotEmpty(); });
        When(x => x.AccountId > 0 && !string.IsNullOrWhiteSpace(x.ApiKey), () => RuleFor(x => x.ApiSecret).NotEmpty());
        When(x => x.AccountId > 0 && !string.IsNullOrWhiteSpace(x.ApiSecret), () => RuleFor(x => x.ApiKey).NotEmpty());
    }
}
public class SaveBybitCredentialsCommandHandler : IRequestHandler<SaveBybitCredentialsCommand, Response>
{
    private readonly DataContext _context; private readonly IBybitCredentialSetService _credentials;
    public SaveBybitCredentialsCommandHandler(api.AzureKeyVault.IKeyVaultService vault, DataContext context, ILogger<SaveBybitCredentialsCommandHandler> logger)
    {
        _context = context;
        _credentials = new BybitCredentialSetService(context, vault, NullLogger<BybitCredentialSetService>.Instance);
    }
    public async Task<Response> Handle(SaveBybitCredentialsCommand request, CancellationToken cancellationToken)
    {
        var validation = await new SaveBybitCredentialsCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return new("Validation failed", false, validation.Errors.Select(x => x.ErrorMessage).ToList());
        var accountId = request.AccountId;
        Account? createdAccount = null;
        if (accountId == 0)
        {
            if (await _context.Accounts.AnyAsync(x => x.UserId == request.UserId && x.Name == request.Name && !x.IsDeleted, cancellationToken)) return new($"An account with the name '{request.Name}' already exists", false, 400);
            var account = new Account(request.Name!, request.UserId, EAccountType.Exchange, "Bybit", request.ExternalId);
            _context.Accounts.Add(account);
            try { await _context.SaveChangesAsync(cancellationToken); } catch { return new("Failed to create account", false, 500); }
            accountId = account.Id;
            createdAccount = account;
        }
        else
        {
            var account = await _context.Accounts.SingleOrDefaultAsync(x => x.Id == accountId && x.UserId == request.UserId && !x.IsDeleted, cancellationToken);
            if (account == null) return new("Account not found", false, 404);
            if (account.AccountType != EAccountType.Exchange || account.Exchange != "Bybit") return new("Account is not an active Bybit exchange account", false, 400);
        }
        var replacements = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(request.ApiKey)) { replacements["api-key"] = request.ApiKey; replacements["api-secret"] = request.ApiSecret; }
        if (!string.IsNullOrWhiteSpace(request.WebhookSecret)) replacements["webhook-secret"] = request.WebhookSecret;
        if (replacements.Count == 0) return new("No credential changes supplied", true);
        var result = await _credentials.ReplaceAsync(request.UserId, accountId, replacements, cancellationToken);
        if (result.Success) return new("Credentials saved successfully", true);
        if (createdAccount != null)
        {
            _context.Accounts.Remove(createdAccount);
            try { await _context.SaveChangesAsync(cancellationToken); } catch { }
        }
        return result.Unavailable ? new(api.AzureKeyVault.KeyVaultSecretReadResult.UnavailableMessage, false, 503) : new("Failed to save credentials; recovery may be required", false, 500);
    }
    public static string BuildKey(int userId, int accountId, string suffix) => BybitCredentialKeys.LegacyAccountKey(userId, accountId, suffix);
}
