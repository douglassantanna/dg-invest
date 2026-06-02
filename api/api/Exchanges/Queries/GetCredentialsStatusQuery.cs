using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Commands;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetCredentialsStatusQuery(int UserId) : IRequest<Response>;

public record CredentialsStatusDto(
    int AccountId,
    string AccountTag,
    bool HasApiKey,
    bool HasApiSecret,
    bool HasWebhookSecret);

public class GetCredentialsStatusQueryHandler : IRequestHandler<GetCredentialsStatusQuery, Response>
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;

    public GetCredentialsStatusQueryHandler(IKeyVaultService keyVaultService, DataContext context)
    {
        _keyVaultService = keyVaultService;
        _context = context;
    }

    public async Task<Response> Handle(GetCredentialsStatusQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == request.UserId)
            .OrderBy(a => a.SubaccountTag)
            .ToListAsync(cancellationToken);

        var results = new List<CredentialsStatusDto>();
        foreach (var account in accounts)
        {
            var apiKey = await _keyVaultService.GetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, account.Id, "api-key"));
            var apiSecret = await _keyVaultService.GetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, account.Id, "api-secret"));
            var webhookSecret = await _keyVaultService.GetSecretAsync(
                SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, account.Id, "webhook-secret"));

            results.Add(new CredentialsStatusDto(
                AccountId: account.Id,
                AccountTag: account.SubaccountTag,
                HasApiKey: !string.IsNullOrEmpty(apiKey),
                HasApiSecret: !string.IsNullOrEmpty(apiSecret),
                HasWebhookSecret: !string.IsNullOrEmpty(webhookSecret)));
        }

        return new Response("ok", true, results);
    }
}
