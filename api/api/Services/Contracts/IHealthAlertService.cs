namespace api.Services.Contracts;

public interface IHealthAlertService
{
    ValueTask AlertAsync(
            string subject,
            string body,
            CancellationToken cancellationToken = default);
}
