namespace VaultLedger.Domain.Enums;

// Ordered low → critical so comparisons like `>= High` work as expected.
public enum CasePriority
{
    Low,
    Medium,
    High,
    Critical,
}
