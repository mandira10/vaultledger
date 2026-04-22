using Microsoft.EntityFrameworkCore;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<User> Users { get; }

    DbSet<TenantMembership> TenantMemberships { get; }

    DbSet<TenantIntegration> TenantIntegrations { get; }

    DbSet<Entity> Entities { get; }

    DbSet<Case> Cases { get; }

    DbSet<AuditEntry> AuditEntries { get; }

    DbSet<ComplianceReview> ComplianceReviews { get; }

    DbSet<IntegrationEvent> IntegrationEvents { get; }

    DbSet<AiInteraction> AiInteractions { get; }

    DbSet<AiUsageQuota> AiUsageQuotas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
