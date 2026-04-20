namespace VaultLedger.Domain.Enums;

// Classifies the compliance subject stored in the `entities` table.
// Not to be confused with the DDD notion of an entity.
public enum EntityType
{
    Individual,
    Company,
    Vendor,
}
