using System;
using System.Collections.Generic;
using api.Models.Cryptos;

namespace function_api.Cryptos.Dtos;
public record ViewMinimalCryptoAssetDto(int Id,
                                        string CurrencyName,
                                        string CryptoCurrency,
                                        string Symbol,
                                        decimal AveragePrice);

public record ViewCryptoAssetDto(int Id,
                                 string CurrencyName,
                                 string CryptoCurrency,
                                 string Symbol,
                                 DateTimeOffset CreatedAt,
                                 IReadOnlyCollection<ViewCryptoTransactionDto> Transactions,
                                 decimal Balance,
                                 IReadOnlyCollection<ViewAddressDto> Addresses,
                                 decimal AveragePrice,
                                 decimal TotalSpent);

public record ViewCryptoTransactionDto(decimal Amount,
                                       decimal Price,
                                       DateTimeOffset PurchaseDate,
                                       string ExchangeName,
                                       ETransactionType TransactionType);

public record ViewAddressDto(int Id,
                             string AddressName,
                             string AddressValue);