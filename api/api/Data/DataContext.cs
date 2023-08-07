using api.Models.Cryptos;
using Microsoft.EntityFrameworkCore;

namespace api.Data;
public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }
    public DbSet<CryptoWallet> CryptoWallets { get; set; } = null!;
    public DbSet<CryptoTransaction> CryptoTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CryptoWallet>().Property(c => c.CryptoCurrency).HasMaxLength(255);
        builder.Entity<CryptoWallet>().Property(c => c.Symbol).HasMaxLength(255);
        builder.Entity<CryptoWallet>().Property(c => c.CurrencyName).HasMaxLength(255);
        builder.Entity<CryptoWallet>().Property(c => c.Balance).HasPrecision(18, 8);
        builder.Entity<CryptoWallet>().Property(c => c.AveragePrice).HasPrecision(18, 8);

        builder.Entity<CryptoTransaction>().Property(c => c.Amount).HasPrecision(18, 8);
        builder.Entity<CryptoTransaction>().Property(c => c.Price).HasPrecision(18, 8);
        builder.Entity<CryptoTransaction>().Property(c => c.ExchangeName).HasMaxLength(255);
    }

}
