using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Domain.Entities;

// Immutable meta-audit of every AI call. No mutators — once written, final.
public class AiInteraction : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public AiInteractionType InteractionType { get; private set; }

    public AiModelProvider ModelProvider { get; private set; }

    public string Model { get; private set; } = null!;

    public int PromptTokens { get; private set; }

    public int CompletionTokens { get; private set; }

    public decimal CostUsd { get; private set; }

    private AiInteraction() { }

    public AiInteraction(
        Guid tenantId,
        Guid userId,
        AiInteractionType interactionType,
        AiModelProvider modelProvider,
        string model,
        int promptTokens,
        int completionTokens,
        decimal costUsd)
    {
        TenantId = tenantId;
        UserId = userId;
        InteractionType = interactionType;
        ModelProvider = modelProvider;
        Model = model;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        CostUsd = costUsd;
    }
}
