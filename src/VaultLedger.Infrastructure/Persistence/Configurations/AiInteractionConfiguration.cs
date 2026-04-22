using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

internal sealed class AiInteractionConfiguration : IEntityTypeConfiguration<AiInteraction>
{
    public void Configure(EntityTypeBuilder<AiInteraction> builder)
    {
        builder.ToTable("ai_interactions");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(i => i.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(i => i.InteractionType)
            .HasColumnName("interaction_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.ModelProvider)
            .HasColumnName("model_provider")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Model)
            .HasColumnName("model")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.PromptTokens).HasColumnName("prompt_tokens").IsRequired();

        builder.Property(i => i.CompletionTokens).HasColumnName("completion_tokens").IsRequired();

        // numeric(10,6) covers up to $9999.999999 per interaction — plenty.
        builder.Property(i => i.CostUsd)
            .HasColumnName("cost_usd")
            .HasPrecision(10, 6)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.TenantId, i.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_ai_interactions_tenant_created");

        builder.HasIndex(i => new { i.TenantId, i.InteractionType, i.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_ai_interactions_type");
    }
}
