using api.Models.Cryptos;
using api.SpotSolar.Models;
using api.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;
public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
    : base(options)
    {
    }

    public DbSet<CryptoTransaction> CryptoTransactions { get; set; } = null!;
    public DbSet<CryptoAsset> CryptoAssets { get; set; } = null!;
    public DbSet<Proposal> Proposals { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Proposal>().OwnsOne(x => x.Customer, c =>
         {
             c.Property(x => x.Email).HasColumnType("varchar").HasMaxLength(255);
             c.Property(x => x.Name).HasColumnType("varchar").HasMaxLength(255);
             c.Property(x => x.Phone).HasColumnType("varchar").HasMaxLength(255);
         });

        modelBuilder.Entity<Proposal>().OwnsOne(x => x.Address, a =>
        {
            a.Property(x => x.Street).HasColumnType("varchar").HasMaxLength(255);
            a.Property(x => x.Number).HasColumnType("varchar").HasMaxLength(255);
            a.Property(x => x.City).HasColumnType("varchar").HasMaxLength(255);
            a.Property(x => x.State).HasColumnType("varchar").HasMaxLength(255);
            a.Property(x => x.ZipCode).HasColumnType("varchar").HasMaxLength(255);
        });

        modelBuilder.Entity<Proposal>().OwnsMany(x => x.Products, p =>
        {
            p.Property(x => x.Name).HasColumnType("varchar").HasMaxLength(255);
        });

        modelBuilder.Entity<Proposal>().Property(x => x.Notes).HasColumnType("varchar").HasMaxLength(255);
        modelBuilder.Entity<Proposal>().Property(x => x.Power).HasColumnType("varchar").HasMaxLength(255);
        modelBuilder.Entity<Proposal>().Property(x => x.TotalPrice).HasPrecision(18, 8);
        modelBuilder.Entity<Proposal>().Property(x => x.TotalPriceProducts).HasPrecision(18, 8);
        modelBuilder.Entity<Proposal>().Property(x => x.LabourValue).HasPrecision(18, 8);
    }

}



