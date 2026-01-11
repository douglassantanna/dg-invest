namespace api.Services.Contracts;

public interface IHealthAlertService
{
    ValueTask AlertAsync(
            string subject,
            string body,
            Exception? exception = null,
            CancellationToken cancellationToken = default);
}
