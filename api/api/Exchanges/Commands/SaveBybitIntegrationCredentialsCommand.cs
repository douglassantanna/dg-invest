using api.Exchanges.Services;
using api.Shared;
using FluentValidation;
using MediatR;

namespace api.Exchanges.Commands;
public record SaveBybitIntegrationCredentialsCommand(int UserId, string ApiKey, string ApiSecret) : IRequest<Response>;
public class SaveBybitIntegrationCredentialsCommandValidator : AbstractValidator<SaveBybitIntegrationCredentialsCommand>
{
    public SaveBybitIntegrationCredentialsCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0); RuleFor(x => x.ApiKey).NotEmpty().MaximumLength(255); RuleFor(x => x.ApiSecret).NotEmpty().MaximumLength(255);
    }
}
public class SaveBybitIntegrationCredentialsCommandHandler : IRequestHandler<SaveBybitIntegrationCredentialsCommand, Response>
{
    private readonly IBybitCredentialSetService _credentials;
    public SaveBybitIntegrationCredentialsCommandHandler(IBybitCredentialSetService credentials) => _credentials = credentials;
    public async Task<Response> Handle(SaveBybitIntegrationCredentialsCommand request, CancellationToken cancellationToken)
    {
        var validation = await new SaveBybitIntegrationCredentialsCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return new("Validation failed", false, validation.Errors.Select(x => x.ErrorMessage).ToList());
        var result = await _credentials.ReplaceAsync(request.UserId, null, new Dictionary<string, string> { ["api-key"] = request.ApiKey, ["api-secret"] = request.ApiSecret }, cancellationToken);
        if (result.Success) return new("Integration credentials saved successfully", true);
        return result.Unavailable ? new(api.AzureKeyVault.KeyVaultSecretReadResult.UnavailableMessage, false, 503) : new("Failed to save integration credentials; recovery may be required", false, 500);
    }
    public static string BuildIntegrationKey(int userId, string suffix) => BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);
}
