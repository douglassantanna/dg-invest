using System;
using System.Collections.Generic;
using api.Models.Cryptos;

namespace function_api.Cryptos.Dtos;
public record ViewMinimalCryptoAssetDto(int Id,
                                        string CurrencyName,
                                        string CryptoCurrency,
                                        string Symbol);

public record ViewCryptoAssetDto(int Id,
                                 string CurrencyName,
                                 string CryptoCurrency,
                                 string Symbol,
                                 DateTimeOffset CreatedAt,
                                 IReadOnlyCollection<ViewCryptoTransactionDto> Transactions,
                                 decimal Balance,
                                 IReadOnlyCollection<string> Addresses,
                                 decimal AveragePrice);

public record ViewCryptoTransactionDto(decimal Amount,
                                       decimal Price,
                                       DateTimeOffset PurchaseDate,
                                       string ExchangeName,
                                       ETransactionType TransactionType);