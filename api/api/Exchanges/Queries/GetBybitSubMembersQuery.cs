using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Services;
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
    int? AccountId)
{
    public string? MappedAccountTag => MappedAccountName;
}

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
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(x => x.UserId == request.UserId && x.Exchange == "Bybit", cancellationToken);
        if (integration == null)
            return new Response("Bybit integration credentials not found. Please save your API key and secret first.", false, 400);

        var apiKey = await BybitCredentialReader.ReadAsync(_context, _keyVaultService, request.UserId, null, "api-key", cancellationToken);
        var apiSecret = await BybitCredentialReader.ReadAsync(_context, _keyVaultService, request.UserId, null, "api-secret", cancellationToken);

        if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
            return new Response(KeyVaultSecretReadResult.UnavailableMessage, false, 503);

        if (string.IsNullOrEmpty(apiKey.Value) || string.IsNullOrEmpty(apiSecret.Value))
            return new Response("Bybit integration credentials not found. Please save your API key and secret first.", false, 400);

        List<BybitSubMember> subMembers;
        try
        {
            subMembers = await _bybitService.GetSubAccountsAsync(apiKey.Value!, apiSecret.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBybitSubMembers: failed to fetch from Bybit for user {UserId}", request.UserId);
            return new Response("Failed to fetch sub-accounts from Bybit", false, 500);
        }

        // Load existing account ExternalId mappings to show which are already linked.
        var mappedAccounts = await _context.Accounts
            .Where(a => a.UserId == request.UserId && a.ExternalId != null && !a.IsDeleted
                     && a.AccountType == api.Cryptos.Models.EAccountType.Exchange && a.Exchange == "Bybit")
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
