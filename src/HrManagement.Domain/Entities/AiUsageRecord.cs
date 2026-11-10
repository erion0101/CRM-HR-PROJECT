namespace HrManagement.Domain;

public sealed class AiUsageRecord : Entity
{
    private AiUsageRecord() { }

    public Guid UserId { get; private set; }
    public Guid MessageId { get; private set; }
    public string Model { get; private set; } = string.Empty;
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public decimal EstimatedCostUsd { get; private set; }

    public static AiUsageRecord Create(Guid organizationId, Guid userId, Guid messageId, string model, int inputTokens, int outputTokens, decimal estimatedCostUsd) => new()
    {
        OrganizationId = organizationId,
        UserId = userId,
        MessageId = messageId,
        Model = Guard.Required(model, nameof(model), 100),
        InputTokens = Math.Max(0, inputTokens),
        OutputTokens = Math.Max(0, outputTokens),
        EstimatedCostUsd = Math.Max(0, estimatedCostUsd)
    };
}
