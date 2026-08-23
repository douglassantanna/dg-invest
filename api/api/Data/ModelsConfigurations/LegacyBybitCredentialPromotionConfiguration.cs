using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;

public class LegacyBybitCredentialPromotionConfiguration : IEntityTypeConfiguration<LegacyBybitCredentialPromotion>
{
    public void Configure(EntityTypeBuilder<LegacyBybitCredentialPromotion> builder)
    {
        builder.ToTable("LegacyBybitCredentialPromotions");
        builder.Property(x => x.Exchange).HasMaxLength(50).IsRequired();
        builder.Property(x => x.State).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CredentialOperationId).HasMaxLength(32);
        builder.Property(x => x.CredentialSetId).HasMaxLength(32);
        builder.HasIndex(x => new { x.UserId, x.Exchange }).IsUnique();
    }
}
