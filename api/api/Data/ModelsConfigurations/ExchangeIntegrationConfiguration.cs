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
        builder.Property(x => x.LastErrorCode).HasColumnType("varchar").HasMaxLength(50);
        builder.Property(x => x.LastErrorMessage).HasColumnType("nvarchar").HasMaxLength(1000);
        builder.Property(x => x.LastErrorEndpoint).HasColumnType("varchar").HasMaxLength(200);
        builder.Property(x => x.ConsecutiveTransportFailures).HasDefaultValue(0);
        builder.Property(x => x.PauseNotificationAttempts).HasDefaultValue(0);
        builder.HasIndex(x => new { x.UserId, x.Exchange }).IsUnique();
        builder.HasOne(x => x.MasterAccount)
            .WithMany()
            .HasForeignKey(x => x.MasterAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MasterAccountId)
            .IsUnique()
            .HasFilter("[MasterAccountId] IS NOT NULL");
    }
}
