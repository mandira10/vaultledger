using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Interfaces;

namespace VaultLedger.Infrastructure.Services;

// Scoped per HTTP request. Populated by TenantContextMiddleware from JWT claims.
internal sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Role Role { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public void SetContext(Guid tenantId, Guid userId, Role role)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
        IsAuthenticated = true;
    }
}
