using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Common.Interfaces;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Interfaces;

namespace VaultLedger.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<TenantIntegration> TenantIntegrations => Set<TenantIntegration>();

    public DbSet<Entity> Entities => Set<Entity>();

    public DbSet<Case> Cases => Set<Case>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<ComplianceReview> ComplianceReviews => Set<ComplianceReview>();

    public DbSet<IntegrationEvent> IntegrationEvents => Set<IntegrationEvent>();

    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();

    public DbSet<AiUsageQuota> AiUsageQuotas => Set<AiUsageQuota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discovers IEntityTypeConfiguration<T> classes in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantQueryFilters(modelBuilder);
    }

    // Adds a WHERE tenant_id = @currentTenant filter to every ITenantScoped entity.
    // Callers can opt out per-query with .IgnoreQueryFilters() when explicitly needed.
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdOnEntity = Expression.Property(
                param,
                nameof(ITenantScoped.TenantId));
            var tenantIdOnContext = Expression.Property(
                Expression.Constant(_tenantContext),
                nameof(ITenantContext.TenantId));
            var equal = Expression.Equal(tenantIdOnEntity, tenantIdOnContext);
            var lambda = Expression.Lambda(equal, param);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
