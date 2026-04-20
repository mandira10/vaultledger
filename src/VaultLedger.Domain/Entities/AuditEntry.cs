using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

public class AuditEntry : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid CaseId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public EntryType EntryType { get; private set; }

    public string Body { get; private set; } = null!;

    public Severity Severity { get; private set; }

    private AuditEntry() { }

    public AuditEntry(
        Guid tenantId,
        Guid caseId,
        Guid createdBy,
        EntryType entryType,
        string body,
        Severity severity)
    {
        TenantId = tenantId;
        CaseId = caseId;
        CreatedBy = createdBy;
        EntryType = entryType;
        Body = body;
        Severity = severity;
    }

    // Intentionally no mutator methods. Audit entries are append-only —
    // immutability is also enforced at the app, API and DB layers.
}
