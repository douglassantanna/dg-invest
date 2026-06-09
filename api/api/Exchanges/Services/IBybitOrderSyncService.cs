using api.Cryptos.Models;
using api.Exchanges.Bybit;

namespace api.Exchanges.Services;

public interface IBybitOrderSyncService
{
    Task ProcessOrderAsync(BybitOrderData order, Account account, int userId, string importSource, CancellationToken cancellationToken);
    Task ProcessDepositAsync(BybitDepositWithdrawalRow deposit, Account account, int userId, CancellationToken cancellationToken);
    Task ProcessWithdrawalAsync(BybitDepositWithdrawalRow withdrawal, Account account, int userId, CancellationToken cancellationToken);
    Task UpsertSyncStatusAsync(int userId, int accountId, string? lastOrderId, CancellationToken cancellationToken);
    Task MarkSyncStatusErrorAsync(int userId, int accountId, string errorMessage, CancellationToken cancellationToken);
}
