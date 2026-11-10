using System.Text.Json;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai;

public sealed record AiCapabilitiesDto(
    bool Enabled,
    bool GovernanceApproved,
    bool ProviderConfigured,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> SuggestedPrompts,
    string? DisabledReason);

public sealed record AiConversationDto(
    Guid Id,
    string Title,
    string? ContextType,
    Guid? ContextId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<AiConversationMessageDto> Messages);

public sealed record AiConversationMessageDto(
    Guid Id,
    AiMessageRole Role,
    AiMessageStatus Status,
    string Content,
    AiResultType ResultType,
    JsonElement? Result,
    AiEvidenceStatus EvidenceStatus,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<AiSourceDto> Sources);

public sealed record AiSourceDto(
    string Id,
    string Kind,
    string Label,
    string? EntityType,
    Guid? EntityId,
    Guid? DocumentId,
    string? Location,
    string? Url);

public sealed record AiMessageResponse(
    Guid ConversationId,
    Guid MessageId,
    AiMessageStatus Status,
    string Message,
    AiStructuredResult Result,
    AiEvidenceStatus EvidenceStatus,
    IReadOnlyList<AiSourceDto> Sources,
    IReadOnlyList<string> SuggestedPrompts);

public sealed record AiStructuredResult(AiResultType Type, JsonElement Items);

public sealed record AiSettingsDto(
    bool Enabled,
    bool RealDataApproved,
    bool ProviderConfigured,
    string Provider,
    string ChatModel,
    string EmbeddingModel,
    int ConversationRetentionDays,
    int RequestsPerDayPerUser,
    int RequestsPerDayPerOrganization,
    decimal MonthlyBudgetUsd);

public sealed record AiUsageSummaryDto(int RequestsToday, int InputTokensToday, int OutputTokensToday, decimal CostThisMonthUsd);

public sealed record DocumentAiStatusDto(
    Guid DocumentId,
    Guid DocumentVersionId,
    DataClassification Classification,
    bool Approved,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAtUtc,
    DocumentAiIndexStatus Status,
    int Attempts,
    string? ErrorCode,
    DateTimeOffset? ProcessedAtUtc);

public sealed record AiProviderMessage(string Role, string Content);
public sealed record AiToolDefinition(string Name, string Description, string JsonSchema);
public sealed record AiToolCall(string CallId, string Name, string ArgumentsJson);
public sealed record AiToolExchange(AiToolCall Call, string OutputJson);
public sealed record AiProviderTurn(IReadOnlyList<string> OpaqueOutputItems, IReadOnlyList<AiToolExchange> ToolExchanges);
public sealed record AiProviderRequest(
    string SystemPrompt,
    IReadOnlyList<AiProviderMessage> Messages,
    IReadOnlyList<AiToolDefinition> Tools,
    IReadOnlyList<AiToolExchange> Exchanges,
    int MaxOutputTokens,
    IReadOnlyList<AiProviderTurn>? ProviderTurns = null,
    TimeSpan? Timeout = null);
public sealed record AiProviderResponse(
    string Text,
    IReadOnlyList<string> ReferencedSourceIds,
    IReadOnlyList<AiToolCall> ToolCalls,
    int InputTokens,
    int OutputTokens,
    string Model,
    IReadOnlyList<string>? OpaqueOutputItems = null);

public sealed record AiEvidence(
    string Id,
    string Kind,
    string Label,
    string? EntityType,
    Guid? EntityId,
    Guid? EmployeeId,
    Guid? DocumentId,
    Guid? DocumentVersionId,
    Guid? DocumentChunkId,
    string? Location,
    string? Url);

public sealed record AiToolResult(AiResultType ResultType, JsonElement Data, IReadOnlyList<AiEvidence> Sources, int ResultCount);

public sealed record AiEmployeeSearch(
    string? Search,
    string? Position,
    string? OrganizationUnit,
    string? EmploymentStatus,
    Guid? OrganizationUnitId,
    Guid? ManagerEmployeeId,
    DateOnly? HiredFrom,
    DateOnly? HiredBefore,
    int? BirthMonth,
    int? BirthDay,
    int Limit);

public sealed record AiEmployeeProfileRequest(Guid EmployeeId, IReadOnlyList<string> Fields);

public sealed record AiContractSearch(
    Guid? EmployeeId,
    IReadOnlyList<Guid>? EmployeeIds,
    string? Status,
    DateOnly? StartsFrom,
    DateOnly? StartsBefore,
    DateOnly? EndsFrom,
    DateOnly? EndsBefore,
    Guid? OrganizationUnitId,
    int Limit);

public sealed record AiDocumentSearch(
    Guid? EmployeeId,
    IReadOnlyList<Guid>? EmployeeIds,
    string? Search,
    string? DocumentType,
    int Limit);

public sealed record AiKnowledgeSearch(string Search, Guid? ProjectId, int Limit);

public sealed record AiExecutionContext(
    Guid UserId,
    Guid OrganizationId,
    Guid? LinkedEmployeeId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> AuthorizedEmployeeIds,
    string CorrelationId,
    DateOnly CurrentDate,
    string OrganizationTimeZone);
