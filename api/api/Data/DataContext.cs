using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;
public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
    : base(options)
    {
    }

    public virtual DbSet<CryptoTransaction> CryptoTransactions { get; set; } = null!;
    public virtual DbSet<CryptoAsset> CryptoAssets { get; set; } = null!;
    public virtual DbSet<Crypto> Cryptos { get; set; } = null!;
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<Account> Accounts { get; set; } = null!;
    public virtual DbSet<AccountTransaction> AccountTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
