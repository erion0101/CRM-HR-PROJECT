namespace HrManagement.Domain;

public sealed class AiToolExecution : Entity
{
    private AiToolExecution() { }

    public Guid MessageId { get; private set; }
    public string ToolName { get; private set; } = string.Empty;
    [AuditRedacted] public string? InputSummary { get; private set; }
    public long DurationMilliseconds { get; private set; }
    public int ResultCount { get; private set; }
    public bool Succeeded { get; private set; }
    public string? FailureCode { get; private set; }

    public static AiToolExecution Create(Guid organizationId, Guid messageId, string toolName, string? inputSummary, long durationMilliseconds, int resultCount, bool succeeded, string? failureCode) => new()
    {
        OrganizationId = organizationId,
        MessageId = messageId,
        ToolName = Guard.Required(toolName, nameof(toolName), 80),
        InputSummary = inputSummary,
        DurationMilliseconds = Math.Max(0, durationMilliseconds),
        ResultCount = Math.Max(0, resultCount),
        Succeeded = succeeded,
        FailureCode = failureCode
    };
}
