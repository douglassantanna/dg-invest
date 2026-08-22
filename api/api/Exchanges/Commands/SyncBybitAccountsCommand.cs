using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record SyncBybitAccountsCommand(int UserId) : IRequest<Response>;

public class SyncBybitAccountsCommandHandler : IRequestHandler<SyncBybitAccountsCommand, Response>
{
    private readonly IBybitService _bybitService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<SyncBybitAccountsCommandHandler> _logger;

    public SyncBybitAccountsCommandHandler(
        IBybitService bybitService,
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<SyncBybitAccountsCommandHandler> logger)
    {
        _bybitService = bybitService;
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(SyncBybitAccountsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                _logger.LogError("SyncBybitAccounts: user {UserId} not found", request.UserId);
                return new Response("User not found", false, 404);
            }

            var integration = await _context.ExchangeIntegrations
                .SingleOrDefaultAsync(x => x.UserId == request.UserId && x.Exchange == "Bybit", cancellationToken);
            if (integration == null)
            {
                var legacyMainAccount = await _context.Accounts.SingleOrDefaultAsync(
                    account => account.UserId == request.UserId && !account.IsDeleted && account.Name == "main",
                    cancellationToken);
                if (legacyMainAccount != null)
                {
                    var legacyApiKey = await _keyVaultService.GetSecretReadResultAsync(SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, legacyMainAccount.Id, "api-key"));
                    var legacyApiSecret = await _keyVaultService.GetSecretReadResultAsync(SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, legacyMainAccount.Id, "api-secret"));
                    if (legacyApiKey.IsUnavailable || legacyApiSecret.IsUnavailable)
                        return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);
                    if (!string.IsNullOrEmpty(legacyApiKey.Value) && !string.IsNullOrEmpty(legacyApiSecret.Value))
                        return new Response("Your existing Bybit discovery credentials need migration to the integration model. They remain unchanged while migration is prepared.", false, 409);
                }

                return new Response("Bybit integration credentials not found. Please save your API key and secret first.", false, 400);
            }

            var apiKey = await _keyVaultService.GetSecretReadResultAsync(SaveBybitIntegrationCredentialsCommandHandler.BuildIntegrationKey(request.UserId, "api-key"));
            var apiSecret = await _keyVaultService.GetSecretReadResultAsync(SaveBybitIntegrationCredentialsCommandHandler.BuildIntegrationKey(request.UserId, "api-secret"));

            if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
                return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);

            if (string.IsNullOrEmpty(apiKey.Value) || string.IsNullOrEmpty(apiSecret.Value))
            {
                _logger.LogError("SyncBybitAccounts: Bybit credentials not configured for user {UserId}", request.UserId);
                return new Response("Bybit credentials not found. Please save your API key and secret first.", false, 400);
            }

            List<BybitSubMember> subMembers;
            try
            {
                subMembers = await _bybitService.GetSubAccountsAsync(apiKey.Value!, apiSecret.Value!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncBybitAccounts: failed to fetch sub-accounts from Bybit for user {UserId}", request.UserId);
                return new Response("Failed to fetch sub-accounts from Bybit", false, 500);
            }

            var existingBybitUids = await _context.Accounts
                .Where(a => a.UserId == request.UserId && !a.IsDeleted
                         && a.AccountType == EAccountType.Exchange && a.Exchange == "Bybit" && a.ExternalId != null)
                .ToDictionaryAsync(a => a.ExternalId!, a => a, cancellationToken);

            int created = 0;
            int matched = 0;

            foreach (var member in subMembers)
            {
                if (existingBybitUids.TryGetValue(member.Uid, out var mappedAccount))
                {
                    matched++;
                    _logger.LogInformation("SyncBybitAccounts: UID {Uid} already mapped to account '{Name}'",
                        member.Uid, mappedAccount.Name);
                    continue;
                }

                var tag = string.IsNullOrWhiteSpace(member.Remark)
                    ? member.Username.Trim()
                    : member.Remark.Trim();

                var newAccount = new Account(tag, request.UserId, EAccountType.Exchange, "Bybit", member.Uid);
                _context.Accounts.Add(newAccount);
                created++;
                _logger.LogInformation("SyncBybitAccounts: created account '{Name}' (Bybit UID: {Uid}) for user {UserId}",
                    tag, member.Uid, request.UserId);
            }
            await _context.SaveChangesAsync(cancellationToken);

            return new Response($"Sync complete. {matched} matched, {created} created.", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncBybitAccounts: unexpected error for user {UserId}", request.UserId);
            return new Response("An unexpected error occurred during sync", false, 500);
        }


    }
}
