using api.Cryptos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;

public class UserPortfolioSnapshotConfiguration : IEntityTypeConfiguration<UserPortfolioSnapshot>
{
    public void Configure(EntityTypeBuilder<UserPortfolioSnapshot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => x.Time);
        builder.Property(x => x.Value)
            .HasPrecision(18, 8);
    }
}