using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetBybitSubMembersQuery(int UserId) : IRequest<Response>;

public record BybitSubMemberDto(
    string Uid,
    string Username,
    string Remark,
    string? MappedAccountName,
    int? AccountId);

public class GetBybitSubMembersQueryHandler : IRequestHandler<GetBybitSubMembersQuery, Response>
{
    private readonly IBybitService _bybitService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<GetBybitSubMembersQueryHandler> _logger;

    public GetBybitSubMembersQueryHandler(
        IBybitService bybitService,
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<GetBybitSubMembersQueryHandler> logger)
    {
        _bybitService = bybitService;
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(GetBybitSubMembersQuery request, CancellationToken cancellationToken)
    {
        var mainAccount = await _context.Accounts
            .Where(a => a.UserId == request.UserId
                     && a.Name.ToLower() == "main")
            .FirstOrDefaultAsync(cancellationToken);

        if (mainAccount == null)
            return new Response("Main account not found", false, 404);

        var apiKey = await _keyVaultService.GetSecretAsync(
            SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, mainAccount.Id, "api-key"));
        var apiSecret = await _keyVaultService.GetSecretAsync(
            SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, mainAccount.Id, "api-secret"));

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            return new Response("Bybit credentials not configured for main account", false, 400);

        List<BybitSubMember> subMembers;
        try
        {
            subMembers = await _bybitService.GetSubAccountsAsync(apiKey, apiSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBybitSubMembers: failed to fetch from Bybit for user {UserId}", request.UserId);
            return new Response("Failed to fetch sub-accounts from Bybit", false, 500);
        }

        // Load existing account ExternalId mappings to show which are already linked.
        var mappedAccounts = await _context.Accounts
            .Where(a => a.UserId == request.UserId && a.ExternalId != null && !a.IsDeleted)
            .Select(a => new { a.Id, a.ExternalId, a.Name })
            .ToListAsync(cancellationToken);

        var mappingLookup = mappedAccounts.ToDictionary(a => a.ExternalId!, a => a);

        var result = subMembers.Select(m =>
        {
            var mapped = mappingLookup.TryGetValue(m.Uid, out var entry) ? entry : null;
            return new BybitSubMemberDto(
                Uid: m.Uid,
                Username: m.Username,
                Remark: m.Remark,
                MappedAccountName: mapped?.Name,
                AccountId: mapped?.Id);
        }).ToList();

        return new Response("ok", true, result);
    }
}
