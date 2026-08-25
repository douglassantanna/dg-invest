using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;

public class ExchangeIntegrationConfiguration : IEntityTypeConfiguration<ExchangeIntegration>
{
    public void Configure(EntityTypeBuilder<ExchangeIntegration> builder)
    {
        builder.ToTable("ExchangeIntegrations");
        builder.Property(x => x.Exchange).HasColumnType("varchar").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar").HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Exchange }).IsUnique();
    }
}
