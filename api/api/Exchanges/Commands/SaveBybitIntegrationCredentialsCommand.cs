using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Models;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record SaveBybitIntegrationCredentialsCommand(int UserId, string ApiKey, string ApiSecret) : IRequest<Response>;

public class SaveBybitIntegrationCredentialsCommandValidator : AbstractValidator<SaveBybitIntegrationCredentialsCommand>
{
    public SaveBybitIntegrationCredentialsCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ApiKey).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ApiSecret).NotEmpty().MaximumLength(255);
    }
}

public class SaveBybitIntegrationCredentialsCommandHandler : IRequestHandler<SaveBybitIntegrationCredentialsCommand, Response>
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<SaveBybitIntegrationCredentialsCommandHandler> _logger;

    public SaveBybitIntegrationCredentialsCommandHandler(
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<SaveBybitIntegrationCredentialsCommandHandler> logger)
    {
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(SaveBybitIntegrationCredentialsCommand request, CancellationToken cancellationToken)
    {
        var validation = await new SaveBybitIntegrationCredentialsCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return new Response("Validation failed", false, validation.Errors.Select(x => x.ErrorMessage).ToList());

        try
        {
            var integration = await _context.ExchangeIntegrations
                .SingleOrDefaultAsync(x => x.UserId == request.UserId && x.Exchange == "Bybit", cancellationToken);
            if (integration == null)
            {
                integration = new ExchangeIntegration(request.UserId, "Bybit");
                _context.ExchangeIntegrations.Add(integration);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await _keyVaultService.SetSecretAsync(BuildIntegrationKey(request.UserId, "api-key"), request.ApiKey);
            await _keyVaultService.SetSecretAsync(BuildIntegrationKey(request.UserId, "api-secret"), request.ApiSecret);
            _logger.LogInformation("Bybit integration credentials saved for user {UserId}", request.UserId);
            return new Response("Integration credentials saved successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Bybit integration credentials for user {UserId}", request.UserId);
            return new Response("Failed to save integration credentials", false, 500);
        }
    }

    public static string BuildIntegrationKey(int userId, string suffix) => $"bybit-integration-{userId}-{suffix}";
}
