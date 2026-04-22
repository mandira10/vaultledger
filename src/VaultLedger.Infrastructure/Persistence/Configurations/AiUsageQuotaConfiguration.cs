using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

internal sealed class AiUsageQuotaConfiguration : IEntityTypeConfiguration<AiUsageQuota>
{
    public void Configure(EntityTypeBuilder<AiUsageQuota> builder)
    {
        builder.ToTable("ai_usage_quotas");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id).HasColumnName("id");

        builder.Property(q => q.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(q => q.PeriodStart)
            .HasColumnName("period_start")
            .IsRequired();

        builder.Property(q => q.TokenBudget).HasColumnName("token_budget").IsRequired();

        builder.Property(q => q.TokensUsed)
            .HasColumnName("tokens_used")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(q => q.CostUsd)
            .HasColumnName("cost_usd")
            .HasPrecision(10, 6)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(q => q.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // One quota record per tenant per month.
        builder.HasIndex(q => new { q.TenantId, q.PeriodStart })
            .IsUnique()
            .HasDatabaseName("idx_ai_quotas_tenant_period");
    }
}
