namespace HrManagement.Domain;

public sealed class AiMessage : Entity
{
    private AiMessage() { }

    public Guid ConversationId { get; private set; }
    public AiMessageRole Role { get; private set; }
    public AiMessageStatus Status { get; private set; }
    [AuditRedacted] public string Content { get; private set; } = string.Empty;
    public AiResultType ResultType { get; private set; }
    [AuditRedacted] public string? ResultJson { get; private set; }
    public AiEvidenceStatus EvidenceStatus { get; private set; }
    public string? Provider { get; private set; }
    public string? Model { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }

    public static AiMessage CreateUser(Guid organizationId, Guid conversationId, string content) => new()
    {
        OrganizationId = organizationId,
        ConversationId = conversationId,
        Role = AiMessageRole.User,
        Status = AiMessageStatus.Completed,
        Content = Guard.Required(content, nameof(content), 4_000),
        ResultType = AiResultType.Text,
        EvidenceStatus = AiEvidenceStatus.NotFound
    };

    public static AiMessage CreateAssistant(Guid organizationId, Guid conversationId) => new()
    {
        OrganizationId = organizationId,
        ConversationId = conversationId,
        Role = AiMessageRole.Assistant,
        Status = AiMessageStatus.Pending,
        ResultType = AiResultType.Text,
        EvidenceStatus = AiEvidenceStatus.NotFound
    };

    public void Complete(string content, AiResultType resultType, string? resultJson, AiEvidenceStatus evidenceStatus, string provider, string model, int inputTokens, int outputTokens, DateTimeOffset now)
    {
        Content = Guard.Required(content, nameof(content), 20_000);
        ResultType = resultType;
        ResultJson = resultJson;
        EvidenceStatus = evidenceStatus;
        Provider = Guard.Required(provider, nameof(provider), 40);
        Model = Guard.Required(model, nameof(model), 100);
        InputTokens = Math.Max(0, inputTokens);
        OutputTokens = Math.Max(0, outputTokens);
        Status = AiMessageStatus.Completed;
        Touch(now);
    }

    public void Fail(string safeMessage, DateTimeOffset now)
    {
        Content = Guard.Required(safeMessage, nameof(safeMessage), 1_000);
        ResultType = AiResultType.Error;
        Status = AiMessageStatus.Failed;
        Touch(now);
    }
}
