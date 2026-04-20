using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; private set; } = null!;

    public TenantPlan Plan { get; private set; }

    // EF Core materialisation
    private Tenant() { }

    public Tenant(string name, TenantPlan plan)
    {
        Name = name;
        Plan = plan;
    }

    public void Rename(string name) => Name = name;

    public void ChangePlan(TenantPlan plan) => Plan = plan;
}
