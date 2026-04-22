using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

internal sealed class ComplianceReviewConfiguration : IEntityTypeConfiguration<ComplianceReview>
{
    public void Configure(EntityTypeBuilder<ComplianceReview> builder)
    {
        builder.ToTable("compliance_reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(r => r.CaseId).HasColumnName("case_id").IsRequired();

        builder.Property(r => r.ReviewedBy).HasColumnName("reviewed_by");

        builder.Property(r => r.Summary)
            .HasColumnName("summary")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Comments)
            .HasColumnName("comments")
            .HasColumnType("text");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(r => new { r.TenantId, r.CaseId })
            .HasPrincipalKey(c => new { c.TenantId, c.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Nullable FK — a pending review has no reviewer yet.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.TenantId, r.Status, r.CaseId })
            .HasDatabaseName("idx_reviews_status");
    }
}
