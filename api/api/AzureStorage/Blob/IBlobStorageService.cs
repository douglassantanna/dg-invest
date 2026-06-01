namespace api.AzureStorage.Blob;

public interface IBlobStorageService
{
    Task AppendLogAsync<T>(string containerName, string blobPath, T entry, CancellationToken cancellationToken = default);
    Task<List<T>> ReadLogsAsync<T>(string containerName, string blobPath, CancellationToken cancellationToken = default);
}
