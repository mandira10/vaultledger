namespace VaultLedger.Domain.Enums;

// Ordered info → critical so comparisons like `>= High` work as expected.
public enum Severity
{
    Info,
    Low,
    Medium,
    High,
    Critical,
}
