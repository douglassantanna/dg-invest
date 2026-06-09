using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
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
            var user = await _context.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);


            if (user == null)
            {
                _logger.LogError("SyncBybitAccounts: user {UserId} not found", request.UserId);
                return new Response("User not found", false, 404);
            }

            var mainAccount = user.Accounts.FirstOrDefault(a => a.SubaccountTag.Equals("main", StringComparison.OrdinalIgnoreCase));
            if (mainAccount == null)
            {
                _logger.LogError("SyncBybitAccounts: main account not found for user {UserId}", request.UserId);
                return new Response("Main account not found", false, 404);
            }

            var apiKey = await _keyVaultService.GetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, mainAccount.Id, "api-key"));
            var apiSecret = await _keyVaultService.GetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, mainAccount.Id, "api-secret"));

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                _logger.LogError("SyncBybitAccounts: Bybit credentials not configured for user {UserId}", request.UserId);
                return new Response("Bybit credentials not found. Please save your API key and secret first.", false, 400);
            }

            List<BybitSubMember> subMembers;
            try
            {
                subMembers = await _bybitService.GetSubAccountsAsync(apiKey, apiSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncBybitAccounts: failed to fetch sub-accounts from Bybit for user {UserId}", request.UserId);
                return new Response("Failed to fetch sub-accounts from Bybit", false, 500);
            }

            var existingAccounts = user.Accounts.ToList();
            var existingBybitUids = existingAccounts.Where(a => a.BybitUid != null)
                                                    .ToDictionary(a => a.BybitUid!, a => a);
            var existingTags = existingAccounts.Select(a => a.SubaccountTag.ToLowerInvariant())
                                               .ToHashSet();

            int created = 0;
            int matched = 0;

            foreach (var member in subMembers)
            {
                // 1st priority: match by BybitUid (most reliable — set manually via map-account endpoint).
                if (existingBybitUids.TryGetValue(member.Uid, out var mappedAccount))
                {
                    matched++;
                    _logger.LogInformation("SyncBybitAccounts: UID {Uid} already mapped to account '{Tag}'",
                        member.Uid, mappedAccount.SubaccountTag);
                    continue;
                }

                // 2nd priority: match by remark, 3rd: fall back to auto-generated username.
                var tag = string.IsNullOrWhiteSpace(member.Remark)
                    ? member.Username.Trim()
                    : member.Remark.Trim();

                if (existingTags.Contains(tag.ToLowerInvariant()))
                {
                    var existingAccount = existingAccounts.First(a => a.SubaccountTag.Equals(tag, StringComparison.OrdinalIgnoreCase));
                    existingAccount.SetBybitUid(member.Uid);
                    matched++;
                    _logger.LogInformation("SyncBybitAccounts: sub-account '{Tag}' matched by name, set BybitUid {Uid} for user {UserId}", tag, member.Uid, request.UserId);
                    continue;
                }

                var newAccount = new Account(tag, request.UserId);
                newAccount.SetBybitUid(member.Uid);
                _context.Accounts.Add(newAccount);
                created++;
                _logger.LogInformation("SyncBybitAccounts: created account '{Tag}' (Bybit UID: {Uid}) for user {UserId}",
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
