using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.ModelsConfigurations;
public class CredentialUpdateOperationConfiguration : IEntityTypeConfiguration<CredentialUpdateOperation>
{
    public void Configure(EntityTypeBuilder<CredentialUpdateOperation> builder)
    {
        builder.ToTable("CredentialUpdateOperations");
        builder.Property(x => x.OperationId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NewCredentialSetId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatesAccount).IsRequired();
        builder.Property(x => x.PreviousCredentialSetId).HasMaxLength(32);
        builder.Property(x => x.Exchange).HasMaxLength(50).IsRequired();
        builder.Property(x => x.State).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.OperationId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Exchange, x.AccountId, x.State });
    }
}
