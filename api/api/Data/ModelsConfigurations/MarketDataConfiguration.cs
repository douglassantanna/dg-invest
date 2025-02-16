using api.Cryptos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;

public class MarketDataConfiguration : IEntityTypeConfiguration<MarketDataPoint>
{
    public void Configure(EntityTypeBuilder<MarketDataPoint> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => x.Time);

        builder.Property(x => x.CoinSymbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.CoinPrice)
            .HasPrecision(18, 8);

        builder.Property(x => x.Time)
            .IsRequired();
    }
}