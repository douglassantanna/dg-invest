using api.Data;
using api.HealthCheck;
using api.Services.Contracts;
using api.Shared;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly DataContext _context;
        private readonly IHealthAlertService _alertService;
        private readonly ILogger<HealthCheckService> _logger;
        private readonly DatabaseHealthCheckOptions _options;

        public HealthCheckService(
            DataContext context,
            IHealthAlertService alertService,
            ILogger<HealthCheckService> logger,
            IOptions<DatabaseHealthCheckOptions> optionsAccessor)
        {
            _context = context;
            _alertService = alertService;
            _logger = logger;
            _options = optionsAccessor.Value;
        }

        public async Task<Result<bool>> IsDatabaseHealthyAsync(CancellationToken ct = default)
        {
            try
            {
                bool canConnect = await _context.Database.CanConnectAsync(ct);
                if (!canConnect)
                {
                    const string reason = "Database connection check returned false";
                    await ReportFailureAsync(reason, exception: null, ct);
                    return Result<bool>.Failure(_options.FailureMessage);
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not TaskCanceledException)
            {
                const string reason = "Database connection check failed";
                await ReportFailureAsync(reason, exception: ex, ct);
                return Result<bool>.Failure(_options.FailureMessage);
            }
        }

        private async Task ReportFailureAsync(string reason, Exception? exception, CancellationToken ct)
        {
            _logger.LogError(exception, "Database health check failed: {Reason}", reason);

            string exceptionDetails = exception != null
                ? $"\nException: {exception.GetType().Name}\nMessage: {exception.Message}"
                : string.Empty;

            string body = _options.AlertBodyTemplate
                .Replace("{Reason}", reason)
                .Replace("{ExceptionDetails}", exceptionDetails)
                .Replace("{Timestamp}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"))
                .Replace("{EnvironmentName}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown");

            await _alertService.AlertAsync(_options.AlertSubject, body, ct);
        }
    }
}