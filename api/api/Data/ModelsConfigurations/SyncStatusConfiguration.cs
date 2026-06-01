using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;

public class SyncStatusConfiguration : IEntityTypeConfiguration<SyncStatus>
{
    public void Configure(EntityTypeBuilder<SyncStatus> builder)
    {
        builder.ToTable("SyncStatuses");
        builder.Property(x => x.ExchangeName).HasColumnType("varchar").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar").HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastErrorMessage).HasColumnType("varchar").HasMaxLength(1000);
        builder.Property(x => x.LastOrderId).HasColumnType("varchar").HasMaxLength(100);
        builder.HasIndex(x => new { x.UserId, x.AccountId, x.ExchangeName }).IsUnique();
    }
}
