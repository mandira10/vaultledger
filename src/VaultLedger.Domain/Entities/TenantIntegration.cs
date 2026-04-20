using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

public class TenantIntegration : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public IntegrationProvider Provider { get; private set; }

    public string? EndpointUrl { get; private set; }

    // Encrypted before it reaches the domain — the entity never sees plaintext.
    public string ApiKeyEnc { get; private set; } = null!;

    public bool IsActive { get; private set; }

    private TenantIntegration() { }

    public TenantIntegration(
        Guid tenantId,
        IntegrationProvider provider,
        string apiKeyEnc,
        string? endpointUrl = null)
    {
        TenantId = tenantId;
        Provider = provider;
        ApiKeyEnc = apiKeyEnc;
        EndpointUrl = endpointUrl;
        IsActive = true;
    }

    public void UpdateEndpoint(string? endpointUrl) => EndpointUrl = endpointUrl;

    public void RotateKey(string newApiKeyEnc) => ApiKeyEnc = newApiKeyEnc;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
