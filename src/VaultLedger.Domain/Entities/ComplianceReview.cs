using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

public class ComplianceReview : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid CaseId { get; private set; }

    public Guid? ReviewedBy { get; private set; }

    public string Summary { get; private set; } = null!;

    public ReviewStatus Status { get; private set; }

    public string? Comments { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    private ComplianceReview() { }

    public ComplianceReview(Guid tenantId, Guid caseId, string summary)
    {
        TenantId = tenantId;
        CaseId = caseId;
        Summary = summary;
        Status = ReviewStatus.Pending;
    }

    public void Approve(Guid reviewerId, DateTime now)
    {
        RequirePending();
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        Status = ReviewStatus.Approved;
    }

    public void Reject(Guid reviewerId, DateTime now, string comments)
    {
        RequirePending();
        if (string.IsNullOrWhiteSpace(comments))
            throw new ArgumentException("Rejection requires comments.", nameof(comments));

        ReviewedBy = reviewerId;
        ReviewedAt = now;
        Comments = comments;
        Status = ReviewStatus.Rejected;
    }

    public void RequestRevision(Guid reviewerId, DateTime now, string comments)
    {
        RequirePending();
        if (string.IsNullOrWhiteSpace(comments))
            throw new ArgumentException("Revision request requires comments.", nameof(comments));

        ReviewedBy = reviewerId;
        ReviewedAt = now;
        Comments = comments;
        Status = ReviewStatus.NeedsRevision;
    }

    private void RequirePending()
    {
        if (Status != ReviewStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot decide a review that is already {Status}.");
    }
}
