using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

public class IntegrationEvent : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public IntegrationProvider Provider { get; private set; }

    // Sender's idempotency key — UNIQUE in DB, used to deduplicate retries.
    public string ExternalId { get; private set; } = null!;

    // Mapped to PostgreSQL jsonb by EF Core configuration.
    public string RawPayload { get; private set; } = null!;

    public ProcessingStatus ProcessingStatus { get; private set; }

    private IntegrationEvent() { }

    public IntegrationEvent(
        Guid tenantId,
        IntegrationProvider provider,
        string externalId,
        string rawPayload)
    {
        TenantId = tenantId;
        Provider = provider;
        ExternalId = externalId;
        RawPayload = rawPayload;
        ProcessingStatus = ProcessingStatus.Pending;
    }

    public void MarkProcessed() => ProcessingStatus = ProcessingStatus.Processed;

    public void MarkFailed() => ProcessingStatus = ProcessingStatus.Failed;
}
