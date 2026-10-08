using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Models;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public sealed record ResumeBybitIntegrationCommand(int UserId) : IRequest<Response>;

public sealed class ResumeBybitIntegrationCommandHandler : IRequestHandler<ResumeBybitIntegrationCommand, Response>
{
    private readonly DataContext _context;
    private readonly IKeyVaultService _keyVault;
    private readonly IBybitService _bybitService;

    public ResumeBybitIntegrationCommandHandler(DataContext context, IKeyVaultService keyVault, IBybitService bybitService)
    {
        _context = context;
        _keyVault = keyVault;
        _bybitService = bybitService;
    }

    public async Task<Response> Handle(ResumeBybitIntegrationCommand request, CancellationToken cancellationToken)
    {
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(candidate => candidate.UserId == request.UserId && candidate.Exchange == "Bybit", cancellationToken);
        if (integration is null)
            return new Response("Bybit integration not found", false, 404);

        if (integration.Status != ExchangeIntegration.AutoPausedStatus)
            return new Response("Bybit integration is not auto-paused", false, 400);

        var apiKey = await BybitCredentialReader.ReadAsync(_keyVault, request.UserId, null, "api-key", cancellationToken);
        var apiSecret = await BybitCredentialReader.ReadAsync(_keyVault, request.UserId, null, "api-secret", cancellationToken);
        if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
            return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);
        if (string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
            return new Response("Integration credentials are missing; save credentials before resuming.", false, 400);

        try
        {
            if (!await _bybitService.TestConnectionAsync(apiKey.Value, apiSecret.Value, BybitEndpoints.Parse(integration.Region)))
                return new Response("Bybit connection test failed. The integration remains paused.", false, 400);
        }
        catch (BybitApiException ex)
        {
            return new Response($"Bybit rejected the credentials: {ex.RetCode} - {ex.RetMsg}. The integration remains paused.", false, 400);
        }

        integration.ResumeAfterReview();
        await _context.SaveChangesAsync(cancellationToken);
        return new Response("Bybit connection verified and synchronization resumed.", true);
    }
}
