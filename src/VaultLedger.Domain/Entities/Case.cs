using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

public class Case : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid EntityId { get; private set; }

    public string Title { get; private set; } = null!;

    public CaseStatus Status { get; private set; }

    public CasePriority Priority { get; private set; }

    // Cached timestamp of the most recent audit entry — avoids subquery
    // when listing cases sorted by activity.
    public DateTime? LastEntryAt { get; private set; }

    private Case() { }

    public Case(Guid tenantId, Guid entityId, string title, CasePriority priority)
    {
        TenantId = tenantId;
        EntityId = entityId;
        Title = title;
        Priority = priority;
        Status = CaseStatus.Open;
    }

    public void Rename(string title) => Title = title;

    public void ChangePriority(CasePriority priority) => Priority = priority;

    public void UpdateStatus(CaseStatus next)
    {
        if (!IsValidTransition(Status, next))
            throw new InvalidOperationException(
                $"Invalid case status transition: {Status} → {next}");

        Status = next;
    }

    public void TouchLastEntry(DateTime timestamp) => LastEntryAt = timestamp;

    private static bool IsValidTransition(CaseStatus from, CaseStatus to) => (from, to) switch
    {
        (CaseStatus.Open,        CaseStatus.UnderReview) => true,
        (CaseStatus.UnderReview, CaseStatus.Open)        => true,
        (CaseStatus.UnderReview, CaseStatus.Closed)      => true,
        (CaseStatus.Closed,      CaseStatus.Open)        => true,
        _ => false,
    };
}
