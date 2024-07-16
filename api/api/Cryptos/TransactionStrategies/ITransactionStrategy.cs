using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies;
public interface ITransactionStrategy
{
    Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null);
    EAccountTransactionType TransactionType { get; }
}
