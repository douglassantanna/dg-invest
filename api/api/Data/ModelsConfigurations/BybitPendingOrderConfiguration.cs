using api.Cryptos.Models;
using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;

public class BybitPendingOrderConfiguration : IEntityTypeConfiguration<BybitPendingOrder>
{
    public void Configure(EntityTypeBuilder<BybitPendingOrder> builder)
    {
        builder.ToTable("BybitPendingOrders");
        builder.Property(x => x.OrderId).HasColumnType("varchar").HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.AccountId, x.OrderId }).IsUnique();
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
