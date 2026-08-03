using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record TestBybitConnectionCommand(int UserId, int AccountId) : IRequest<Response>;

public class TestBybitConnectionCommandHandler : IRequestHandler<TestBybitConnectionCommand, Response>
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly IBybitService _bybitService;
    private readonly DataContext _context;
    private readonly ILogger<TestBybitConnectionCommandHandler> _logger;

    public TestBybitConnectionCommandHandler(
        IKeyVaultService keyVaultService,
        IBybitService bybitService,
        DataContext context,
        ILogger<TestBybitConnectionCommandHandler> logger)
    {
        _keyVaultService = keyVaultService;
        _bybitService = bybitService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(TestBybitConnectionCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Where(a => a.Id == request.AccountId && a.UserId == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            return new Response("Account not found", false, 404);
        }

        var key = SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-key");
        var secret = SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "api-secret");

        var apiKey = await _keyVaultService.GetSecretAsync(key);
        var apiSecret = await _keyVaultService.GetSecretAsync(secret);

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            return new Response("API key and secret are not configured for this account", false, 400);
        }

        var success = await _bybitService.TestConnectionAsync(apiKey, apiSecret);

        if (success)
        {
            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.AccountId == request.AccountId && s.ExchangeName == "Bybit", cancellationToken);

            if (syncStatus != null)
            {
                syncStatus.MarkVerified();
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Bybit connection test succeeded for account {AccountId}", request.AccountId);
            return new Response("Connection successful", true, new { verifiedAt = DateTime.UtcNow });
        }

        return new Response("Connection failed. Check your API key and secret.", false, 400);
    }
}
