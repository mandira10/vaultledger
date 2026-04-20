using VaultLedger.Domain.Common;

namespace VaultLedger.Domain.Entities;

// Global identity — users can belong to multiple tenants via TenantMembership.
// Intentionally not ITenantScoped.
public class User : BaseEntity
{
    public string Email { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    private User() { }

    public User(string email, string name, string passwordHash)
    {
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
    }

    public void UpdateName(string name) => Name = name;

    public void ChangePasswordHash(string newHash) => PasswordHash = newHash;
}
