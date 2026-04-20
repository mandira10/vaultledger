using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

public class TenantMembership : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Role Role { get; private set; }

    private TenantMembership() { }

    public TenantMembership(Guid tenantId, Guid userId, Role role)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }

    public void ChangeRole(Role newRole) => Role = newRole;
}
