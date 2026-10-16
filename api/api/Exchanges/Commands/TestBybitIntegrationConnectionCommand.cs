using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Models;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record TestBybitIntegrationConnectionCommand(int UserId) : IRequest<Response>;

public class TestBybitIntegrationConnectionCommandHandler(
    DataContext context,
    IKeyVaultService keyVaultService,
    IBybitService bybitService)
    : IRequestHandler<TestBybitIntegrationConnectionCommand, Response>
{
    public async Task<Response> Handle(TestBybitIntegrationConnectionCommand request, CancellationToken cancellationToken)
    {
        var integration = await context.ExchangeIntegrations
            .SingleOrDefaultAsync(candidate => candidate.UserId == request.UserId && candidate.Exchange == "Bybit", cancellationToken);

        if (integration is null)
            return new Response("Bybit integration is not configured", false, 404);

        if (!integration.Enabled || integration.Status is "Disconnected" or ExchangeIntegration.AutoPausedStatus)
            return new Response("Bybit integration is disabled. Reconnect it before testing credentials.", false, 400);

        var apiKey = await BybitCredentialReader.ReadAsync(keyVaultService, request.UserId, null, "api-key", cancellationToken);
        var apiSecret = await BybitCredentialReader.ReadAsync(keyVaultService, request.UserId, null, "api-secret", cancellationToken);

        if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
            return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);

        if (string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
            return new Response("Bybit API key and secret are not configured", false, 400);

        if (string.Equals(apiKey.Value, apiSecret.Value, StringComparison.Ordinal))
            return new Response("Stored Bybit credentials appear corrupted (API key and secret are identical). Please reconnect.", false, 400);

        var region = BybitEndpoints.Parse(integration.Region);
        try
        {
            if (await bybitService.TestConnectionAsync(apiKey.Value, apiSecret.Value, region))
            {
                var verifiedAt = DateTime.UtcNow;
                if (integration.MasterAccountId is { } masterAccountId)
                {
                    var syncStatus = await context.SyncStatuses
                        .SingleOrDefaultAsync(status => status.UserId == request.UserId
                            && status.AccountId == masterAccountId
                            && status.ExchangeName == "Bybit", cancellationToken);
                    if (syncStatus is not null)
                    {
                        syncStatus.MarkVerified(verifiedAt);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }

                return new Response("Bybit integration connection successful", true, new { verifiedAt });
            }
        }
        catch (BybitTransportException ex)
        {
            return new Response($"Bybit is temporarily unavailable while testing the integration connection ({ex.Endpoint}). Please try again.", false, 503);
        }
        catch (BybitApiException ex)
        {
            return new Response($"Bybit rejected the integration credentials: {ex.RetCode} - {ex.RetMsg}", false, 400);
        }

        return new Response("Bybit integration connection failed. Check your API key and secret.", false, 400);
    }
}
