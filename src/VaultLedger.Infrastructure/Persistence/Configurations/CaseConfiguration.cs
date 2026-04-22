using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

internal sealed class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("cases");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(c => c.EntityId).HasColumnName("entity_id").IsRequired();

        builder.Property(c => c.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.LastEntryAt)
            .HasColumnName("last_entry_at");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Composite FK prevents cross-tenant corruption at the DB level.
        builder.HasOne<Entity>()
            .WithMany()
            .HasForeignKey(c => new { c.TenantId, c.EntityId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Target for child composite FKs (AuditEntry, ComplianceReview).
        builder.HasAlternateKey(c => new { c.TenantId, c.Id });

        // Inbox query — most recent activity first.
        builder.HasIndex(c => new { c.TenantId, c.Status, c.LastEntryAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_cases_inbox");

        builder.HasIndex(c => new { c.TenantId, c.EntityId })
            .HasDatabaseName("idx_cases_entity");
    }
}
