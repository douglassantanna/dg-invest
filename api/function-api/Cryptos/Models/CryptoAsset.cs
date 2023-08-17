using System;
using System.Collections.Generic;
using System.Linq;

namespace api.Models.Cryptos;
public class CryptoAsset
{
    public int Id { get; private set; }
    public string CryptoCurrency { get; private set; } = string.Empty;
    private decimal _balance;
    public decimal Balance
    {
        get { return _transactions.Select(t => t.Amount).Sum(); }
        set { _balance = value; }
    }
    public decimal AveragePrice
    {
        get { return _transactions.Select(t => t.Price).Average(); }
        private set { }
    }
    private readonly List<string> _addresses = new();
    public string Symbol { get; private set; } = string.Empty;
    public string CurrencyName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    private readonly List<CryptoTransaction> _transactions = new();
    public void AddBalance(decimal amount)
    {
        if (amount > 0.0m)
            _balance += amount;
    }
    public void SubtractBalance(decimal amount)
    {
        if (amount > 0.0m)
            _balance -= amount;
    }
    public CryptoAsset(string cryptoCurrency,
                        string currencyName,
                        string symbol)
    {
        CryptoCurrency = cryptoCurrency;
        CurrencyName = currencyName;
        Symbol = symbol;
    }
    public IReadOnlyCollection<string> GetAddresses() => _addresses.AsReadOnly();
    public IReadOnlyCollection<CryptoTransaction> Transactions => _transactions.AsReadOnly();
    public void AddAddress(string address)
    {
        _addresses.Add(address);
    }
    public void AddTransaction(CryptoTransaction transaction)
    {
        _transactions.Add(transaction);
    }
}
