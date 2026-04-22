using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

internal sealed class TenantIntegrationConfiguration : IEntityTypeConfiguration<TenantIntegration>
{
    public void Configure(EntityTypeBuilder<TenantIntegration> builder)
    {
        builder.ToTable("tenant_integrations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(i => i.Provider)
            .HasColumnName("provider")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.EndpointUrl)
            .HasColumnName("endpoint_url")
            .HasMaxLength(500);

        // Ciphertext. Encryption happens in a service before the entity is constructed.
        builder.Property(i => i.ApiKeyEnc)
            .HasColumnName("api_key_enc")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.TenantId, i.Provider }).IsUnique();
    }
}
