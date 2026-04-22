using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Common.Interfaces;
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
    }
}
