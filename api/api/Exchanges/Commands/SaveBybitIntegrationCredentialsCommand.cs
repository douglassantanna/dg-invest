using api.Exchanges.Bybit;
using api.Exchanges.Services;
using api.Cryptos.Models;
using api.Data;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;
public record SaveBybitIntegrationCredentialsCommand(int UserId, string ApiKey, string ApiSecret, BybitRegion Region = BybitRegion.Global, string? MasterUid = null) : IRequest<Response>;
public class SaveBybitIntegrationCredentialsCommandValidator : AbstractValidator<SaveBybitIntegrationCredentialsCommand>
{
    public SaveBybitIntegrationCredentialsCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);         RuleFor(x => x.ApiKey).NotEmpty().MaximumLength(255); RuleFor(x => x.ApiSecret).NotEmpty().MaximumLength(255);
        When(x => !string.IsNullOrWhiteSpace(x.MasterUid), () =>
            RuleFor(x => x.MasterUid).MaximumLength(50).Matches("^[0-9]+$"));
        RuleFor(x => x.ApiSecret).NotEqual(x => x.ApiKey).WithMessage("API key and secret must be different.");
    }
}
public class SaveBybitIntegrationCredentialsCommandHandler : IRequestHandler<SaveBybitIntegrationCredentialsCommand, Response>
{
    private readonly IBybitCredentialSetService _credentials;
    private readonly DataContext _context;
    public SaveBybitIntegrationCredentialsCommandHandler(IBybitCredentialSetService credentials, DataContext context)
    {
        _credentials = credentials;
        _context = context;
    }
    public async Task<Response> Handle(SaveBybitIntegrationCredentialsCommand request, CancellationToken cancellationToken)
    {
        var validation = await new SaveBybitIntegrationCredentialsCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return new("Validation failed", false, validation.Errors.Select(x => x.ErrorMessage).ToList());
        var result = await _credentials.SaveAsync(request.UserId, null, request.ApiKey, request.ApiSecret, null, request.Region, cancellationToken);
        if (result.Success && !string.IsNullOrWhiteSpace(request.MasterUid))
        {
            var integration = await _context.ExchangeIntegrations
                .SingleAsync(x => x.UserId == request.UserId && x.Exchange == "Bybit", cancellationToken);
            var masterAccount = await _context.Accounts
                .SingleOrDefaultAsync(x => x.UserId == request.UserId
                    && x.AccountType == EAccountType.Exchange
                    && x.Exchange == "Bybit"
                    && x.ExternalId == request.MasterUid
                    && !x.IsDeleted, cancellationToken);

            if (masterAccount is null)
            {
                masterAccount = new Account("Bybit master account", request.UserId, EAccountType.Exchange, "Bybit", request.MasterUid);
                _context.Accounts.Add(masterAccount);
                await _context.SaveChangesAsync(cancellationToken);
            }

            integration.LinkMasterAccount(masterAccount.Id);
            await _context.SaveChangesAsync(cancellationToken);
            var masterCredentialResult = await _credentials.SaveAsync(
                request.UserId,
                masterAccount.Id,
                request.ApiKey,
                request.ApiSecret,
                string.Empty,
                request.Region,
                cancellationToken);
            if (!masterCredentialResult.Success)
                return masterCredentialResult.Unavailable
                    ? new(api.AzureKeyVault.KeyVaultSecretReadResult.UnavailableMessage, false, 503)
                    : new("Failed to save master account credentials; recovery may be required", false, 500);

            return new("Master Bybit account connected successfully", true);
        }
        if (result.Success)
            return new("Integration credentials saved. Add the master Bybit UID to finish setup.", true);
        return result.Unavailable ? new(api.AzureKeyVault.KeyVaultSecretReadResult.UnavailableMessage, false, 503) : new("Failed to save integration credentials; recovery may be required", false, 500);
    }
    public static string BuildIntegrationKey(int userId, string suffix) => BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);
}
