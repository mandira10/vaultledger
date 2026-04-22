using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(e => e.CaseId).HasColumnName("case_id").IsRequired();

        builder.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();

        builder.Property(e => e.EntryType)
            .HasColumnName("entry_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Unbounded — audit bodies can be long narratives.
        builder.Property(e => e.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Composite FK preserves tenant boundary at the DB level.
        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.CaseId })
            .HasPrincipalKey(c => new { c.TenantId, c.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Most frequent query in the app: this case's entries, newest first.
        builder.HasIndex(e => new { e.TenantId, e.CaseId, e.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_entries_case");
    }
}
