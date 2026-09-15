namespace api.AzureStorage.Blob;

public static class BlobLogPaths
{
    public static string Daily(string stream, DateTime timestampUtc) =>
        $"{timestampUtc:yyyy/MM/dd}/{stream}.jsonl";
}
