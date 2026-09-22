using api.AzureStorage;
using api.AzureStorage.Blob;
using api.Exchanges.Models;
using api.Shared;
using MediatR;
using Microsoft.Extensions.Options;

namespace api.Exchanges.Queries;

public record GetSyncLogsQuery(int UserId, int AccountId, string? Date = null) : IRequest<Response>;

public class GetSyncLogsQueryHandler : IRequestHandler<GetSyncLogsQuery, Response>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly AzureStorageSettings _settings;

    public GetSyncLogsQueryHandler(IBlobStorageService blobStorageService, IOptions<AzureStorageSettings> settings)
    {
        _blobStorageService = blobStorageService;
        _settings = settings.Value;
    }

    public async Task<Response> Handle(GetSyncLogsQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedDate))
        {
            return new Response("Invalid date", false);
        }

        var blobPath = BlobLogPaths.Daily("sync", parsedDate.ToUniversalTime());

        var entries = await _blobStorageService.ReadLogsAsync<SyncLogEntry>(
            _settings.LogContainer, blobPath, cancellationToken);

        return new Response("ok", true, entries
            .Where(entry => entry.UserId == request.UserId && entry.AccountId == request.AccountId)
            .ToList());
    }
}
