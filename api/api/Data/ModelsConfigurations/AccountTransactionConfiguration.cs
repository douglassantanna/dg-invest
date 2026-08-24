using api.Cryptos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;
public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 8);
        builder.Property(x => x.ExchangeName).HasColumnType("varchar").HasMaxLength(255);
        builder.Property(x => x.Notes).HasColumnType("varchar").HasMaxLength(255);
        builder.Property(x => x.CryptoCurrentPrice).HasPrecision(18, 8);
        builder.Property(x => x.Fee).HasPrecision(18, 8);
        builder.Property(x => x.ExchangeTransactionId).HasColumnType("varchar").HasMaxLength(100);
        builder.Property(x => x.ExchangeStatus).HasColumnType("varchar").HasMaxLength(50);
        builder.HasIndex(x => x.ExchangeTransactionId);
    }
}

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.Property(x => x.Balance).HasPrecision(18, 8);
        builder.Property(x => x.Name).HasColumnType("varchar").HasMaxLength(255);
        builder.Property(x => x.Exchange).HasColumnType("varchar").HasMaxLength(50);
        builder.Property(x => x.ExternalId).HasColumnType("varchar").HasMaxLength(50);
        builder.HasIndex(x => new { x.UserId, x.Exchange, x.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalId] IS NOT NULL AND [IsDeleted] = 0 AND [AccountType] = 1");
    }
}
