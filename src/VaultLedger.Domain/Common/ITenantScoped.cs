namespace VaultLedger.Domain.Common;

// Marker for entities owned by a tenant. EF Core global query filters are
// applied to any entity implementing this, enforcing tenant isolation
// on every read without manual WHERE clauses.
public interface ITenantScoped
{
    Guid TenantId { get; }
}
