using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace api.AzureStorage.Blob;

public static class BlobLoggingExtensions
{
    public static LoggerConfiguration WriteToBlobLogs(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string stream)
    {
        var settings = configuration.GetSection("AzureStorageSettings");
        var connectionString = settings["ConnectionString"];
        var containerName = settings["LogContainer"] ?? "logs";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return loggerConfiguration;
        }

        return loggerConfiguration.WriteTo.AzureBlobStorage(
            new RenderedCompactJsonFormatter(),
            connectionString,
            LogEventLevel.Information,
            storageContainerName: containerName,
            storageFileName: $"{{yyyy}}/{{MM}}/{{dd}}/{stream}.jsonl",
            contentType: "application/jsonl",
            useUtcTimeZone: true);
    }
}
