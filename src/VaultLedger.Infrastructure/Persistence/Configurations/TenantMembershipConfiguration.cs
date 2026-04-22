using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(m => m.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Deleting a tenant removes its memberships; deleting a user is blocked
        // until memberships are revoked explicitly.
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();

        builder.HasIndex(m => m.UserId);
    }
}
