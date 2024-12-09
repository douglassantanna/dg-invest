using api.Models.Cryptos;

namespace api.Cryptos.Dtos;

public record UserCryptoAssetDto(decimal AccountBalance, ViewMinimalCryptoAssetDto CryptoAssetDto);
public record ViewMinimalCryptoAssetDto(int Id,
                                        string Symbol,
                                        decimal PricePerUnit,
                                        decimal Balance,
                                        decimal InvestedAmount,
                                        decimal CurrentWorth,
                                        decimal InvestmentGainLossValue,
                                        decimal InvestmentGainLossPercentage,
                                        int CoinMarketCapId);

public record ViewCryptoAssetDto(int Id,
                                 ViewCryptoInformation CryptoInformation,
                                 List<CryptoAssetData> CryptoAssetData,
                                 IReadOnlyCollection<ViewCryptoTransactionDto> Transactions,
                                 IReadOnlyCollection<ViewAddressDto> Addresses);

public record ViewCryptoDataDto(int Id, List<CryptoAssetData> CryptoAssetData);

public record ViewCryptoTransactionDto(int Id,
                                       decimal Amount,
                                       decimal Price,
                                       DateTimeOffset PurchaseDate,
                                       string ExchangeName,
                                       ETransactionType TransactionType,
                                       decimal PercentDifference,
                                       decimal Fee);

public record ViewAddressDto(int Id,
                             string AddressName,
                             string AddressValue);

public record ViewCryptoInformation(string Symbol,
                                    int CoinMarketCapId);

public record CryptoAssetData(string Title, decimal Value, decimal? Percent = null);