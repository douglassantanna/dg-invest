using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;

namespace api.AzureStorage.Blob;

public class BlobStorageService : IBlobStorageService
{
    private BlobServiceClient? _blobServiceClient;
    private readonly AzureStorageSettings _settings;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<AzureStorageSettings> settings, ILogger<BlobStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        TryInitializeClient();
    }

    private void TryInitializeClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            _logger.LogWarning("AzureStorage ConnectionString is not configured. Blob storage operations will be skipped.");
            return;
        }

        try
        {
            _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize BlobServiceClient. Blob storage operations will be skipped.");
        }
    }

    public async Task AppendLogAsync<T>(string containerName, string blobPath, T entry, CancellationToken cancellationToken = default)
    {
        if (_blobServiceClient == null)
        {
            _logger.LogDebug("Blob storage not configured, skipping append log to {Container}/{Blob}", containerName, blobPath);
            return;
        }

        try
        {
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var appendBlob = container.GetAppendBlobClient(blobPath);

            if (!await appendBlob.ExistsAsync(cancellationToken))
                await appendBlob.CreateAsync(cancellationToken: cancellationToken);

            var json = JsonSerializer.Serialize(entry) + Environment.NewLine;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await appendBlob.AppendBlockAsync(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append log to blob {Container}/{Blob}", containerName, blobPath);
        }
    }

    public async Task<List<T>> ReadLogsAsync<T>(string containerName, string blobPath, CancellationToken cancellationToken = default)
    {
        var results = new List<T>();

        if (_blobServiceClient == null)
        {
            _logger.LogDebug("Blob storage not configured, skipping read log from {Container}/{Blob}", containerName, blobPath);
            return results;
        }

        try
        {
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobPath);

            if (!await blob.ExistsAsync(cancellationToken))
                return results;

            var response = await blob.DownloadAsync(cancellationToken);
            using var reader = new StreamReader(response.Value.Content);
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var entry = JsonSerializer.Deserialize<T>(line);
                if (entry != null)
                    results.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read logs from blob {Container}/{Blob}", containerName, blobPath);
        }
        return results;
    }
}
