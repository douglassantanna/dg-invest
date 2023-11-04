using api.Models.Cryptos;

namespace api.Cryptos.Dtos;
public record ViewMinimalCryptoAssetDto(int Id,
                                        string CurrencyName,
                                        string CryptoCurrency,
                                        string Symbol,
                                        decimal CurrentPrice,
                                        decimal PercentChange24h);

public record ViewCryptoAssetDto(int Id,
                                 ViewCryptoInformation CryptoInformation,
                                 IReadOnlyCollection<ViewCryptoTransactionDto> Transactions,
                                 IReadOnlyCollection<ViewAddressDto> Addresses);

public record ViewCryptoTransactionDto(decimal Amount,
                                       decimal Price,
                                       DateTimeOffset PurchaseDate,
                                       string ExchangeName,
                                       ETransactionType TransactionType);

public record ViewAddressDto(int Id,
                             string AddressName,
                             string AddressValue);

public record ViewCryptoInformation(string Symbol,
                                    decimal PricePerUnit,
                                    decimal MyAveragePrice,
                                    decimal PercentDifference,
                                    decimal Balance,
                                    decimal InvestedAmount,
                                    decimal CurrentWorth,
                                    decimal InvestmentGainLoss,
                                    int CoinMarketCapId);
