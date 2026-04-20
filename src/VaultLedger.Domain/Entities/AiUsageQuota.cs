using VaultLedger.Domain.Common;

namespace VaultLedger.Domain.Entities;

public class AiUsageQuota : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    // First day of the billing month, UTC.
    public DateTime PeriodStart { get; private set; }

    // long because high-usage Enterprise tenants can exceed int.MaxValue tokens.
    public long TokenBudget { get; private set; }

    public long TokensUsed { get; private set; }

    public decimal CostUsd { get; private set; }

    private AiUsageQuota() { }

    public AiUsageQuota(Guid tenantId, DateTime periodStart, long tokenBudget)
    {
        TenantId = tenantId;
        PeriodStart = periodStart;
        TokenBudget = tokenBudget;
    }

    public bool HasBudget(int tokensNeeded) => TokensUsed + tokensNeeded <= TokenBudget;

    public void RecordUsage(long tokens, decimal cost)
    {
        TokensUsed += tokens;
        CostUsd += cost;
    }

    public void RaiseBudget(long newBudget)
    {
        if (newBudget < TokenBudget)
            throw new InvalidOperationException("Cannot lower an active budget mid-period.");

        TokenBudget = newBudget;
    }
}
