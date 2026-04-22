using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Interfaces;

// Current request's tenant/user context. Populated by middleware from JWT claims.
// Consumed by EF Core query filters, handlers and authorization checks.
public interface ITenantContext
{
    Guid TenantId { get; }

    Guid UserId { get; }

    Role Role { get; }

    bool IsAuthenticated { get; }
}
