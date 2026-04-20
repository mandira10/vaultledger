using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

// "Entity" here means a compliance subject (person / company / vendor),
// not the DDD notion of an entity class.
public class Entity : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = null!;

    public EntityType EntityType { get; private set; }

    public string? ReferenceId { get; private set; }

    public bool IsActive { get; private set; }

    private Entity() { }

    public Entity(Guid tenantId, string name, EntityType entityType, string? referenceId = null)
    {
        TenantId = tenantId;
        Name = name;
        EntityType = entityType;
        ReferenceId = referenceId;
        IsActive = true;
    }

    public void UpdateDetails(string name, string? referenceId)
    {
        Name = name;
        ReferenceId = referenceId;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
