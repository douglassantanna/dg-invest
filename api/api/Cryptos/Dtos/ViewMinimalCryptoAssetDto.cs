using api.Models.Cryptos;

namespace api.Cryptos.Dtos;
public record ViewMinimalCryptoAssetDto(int Id,
                                        string CurrencyName,
                                        string CryptoCurrency,
                                        string Symbol,
                                        decimal CurrentPrice);

public record ViewCryptoAssetDto(int Id,
                                 string CurrencyName,
                                 string CryptoCurrency,
                                 string Symbol,
                                 DateTimeOffset CreatedAt,
                                 IReadOnlyCollection<ViewCryptoTransactionDto> Transactions,
                                 decimal Balance,
                                 IReadOnlyCollection<ViewAddressDto> Addresses,
                                 decimal AveragePrice,
                                 decimal CurrentPrice);

public record ViewCryptoTransactionDto(decimal Amount,
                                       decimal Price,
                                       DateTimeOffset PurchaseDate,
                                       string ExchangeName,
                                       ETransactionType TransactionType);

public record ViewAddressDto(int Id,
                             string AddressName,
                             string AddressValue);