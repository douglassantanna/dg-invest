using api.Data;
using api.Services.Contracts;
using api.Shared;

namespace api.Services
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly DataContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<HealthCheckService> _logger;
        private const string failureMessage = "Database is not healthy";
        private const string alertSubject = "Database Health Check Failed";
        private const string alertBody = "The database health check has failed. Please investigate the issue.";

        public HealthCheckService(DataContext context, IEmailService emailService, ILogger<HealthCheckService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result<bool>> IsDatabaseHealthyAsync(CancellationToken cancellationToken)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    _logger.LogError("Database health check failed: cannot connect.");
                    await _emailService.SendApiDownAlertAsync(alertSubject, alertBody);
                    return Result<bool>.Failure(failureMessage);
                }

                _logger.LogInformation("Database health check passed.");
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Database health check threw an exception.");
                var logMessage = $"Database health check exception: {ex.Message}";
                await _emailService.SendApiDownAlertAsync(alertSubject, logMessage);
                return Result<bool>.Failure(failureMessage);
            }
        }
    }
}