using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai;

public interface IAiRuntimeSettings
{
    AiOptions Value { get; }
    bool ProviderConfigured { get; }
    bool UsagePricingConfigured { get; }
}

public interface IAiExecutionContextProvider
{
    Task<AiExecutionContext> GetAsync(CancellationToken cancellationToken);
}

public interface IAiProvider
{
    Task<AiProviderResponse> CompleteAsync(AiProviderRequest request, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}

public interface IEmbeddingProvider
{
    Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken);
}

public interface IAiOrchestrator
{
    Task<AiMessageResponse> SendAsync(Guid conversationId, string prompt, CancellationToken cancellationToken);
}

public interface IAiToolRegistry
{
    IReadOnlyList<AiToolDefinition> Definitions { get; }
    Task<AiToolResult> ExecuteAsync(string name, string argumentsJson, CancellationToken cancellationToken);
}

public interface IAiAuthorizationService
{
    Task<AiCapabilitiesDto> GetCapabilitiesAsync(CancellationToken cancellationToken);
    Task EnsureDocumentApprovalAllowedAsync(Guid employeeId, DataClassification classification, CancellationToken cancellationToken);
}

public interface IAiSourceAuthorizationService
{
    Task<bool> CanAccessAsync(AiSourceReference source, CancellationToken cancellationToken);
}

public interface IAiUsageService
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken);
    Task RecordAsync(Guid messageId, string model, int inputTokens, int outputTokens, CancellationToken cancellationToken);
}

public interface IAiConversationRepository
{
    void Add(AiConversation conversation);
    void Add(AiMessage message);
    void Add(AiSourceReference source);
    void Add(AiToolExecution execution);
    Task<AiConversation?> GetOwnedAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiConversation>> ListOwnedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AiMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiSourceReference>> GetSourcesAsync(Guid messageId, CancellationToken cancellationToken);
    Task<AiUsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken);
}

public interface IAiDataReader
{
    Task<AiToolResult> SearchEmployeesAsync(AiEmployeeSearch search, CancellationToken cancellationToken);
    Task<AiToolResult> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<AiToolResult> GetEmployeeHistoryAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<AiToolResult> GetDirectReportsAsync(Guid managerEmployeeId, CancellationToken cancellationToken);
    Task<AiToolResult> GetOrganizationStructureAsync(int limit, CancellationToken cancellationToken);
    Task<AiToolResult> SearchContractsAsync(AiContractSearch search, CancellationToken cancellationToken);
    Task<AiToolResult> GetEmployeeContractsAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<AiToolResult> ListEmployeeDocumentsAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<AiToolResult> SearchEmployeeDocumentsAsync(AiDocumentSearch search, CancellationToken cancellationToken);
    Task<AiToolResult> GetMyOnboardingAsync(bool progressOnly, CancellationToken cancellationToken);
    Task<AiToolResult> GetAssignedProjectsAsync(CancellationToken cancellationToken);
    Task<AiToolResult> GetProjectOverviewAsync(Guid projectId, CancellationToken cancellationToken);
    Task<AiToolResult> GetProjectTeamAsync(Guid projectId, CancellationToken cancellationToken);
    Task<AiToolResult> SearchKnowledgeAsync(AiKnowledgeSearch search, CancellationToken cancellationToken);
}

public interface IDocumentTextExtractor
{
    bool Supports(string contentType, string fileName);
    Task<IReadOnlyList<ExtractedDocumentSection>> ExtractAsync(Stream stream, string contentType, string fileName, CancellationToken cancellationToken);
}

public sealed record ExtractedDocumentSection(string Text, int? PageNumber, int? ParagraphIndex, string? Heading);

public interface IDocumentIndexingProcessor
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}

public interface IAiMaintenanceProcessor
{
    Task<int> PurgeExpiredConversationsAsync(CancellationToken cancellationToken);
}

public interface IDocumentAiRepository
{
    Task<EmployeeDocument?> GetAccessibleAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken);
    Task<DocumentVersion> GetCurrentVersionAsync(EmployeeDocument document, CancellationToken cancellationToken);
    Task<DocumentAiIndex?> GetIndexAsync(Guid documentVersionId, CancellationToken cancellationToken);
    void Add(DocumentAiIndex index);
    Task DeleteChunksAsync(Guid documentId, CancellationToken cancellationToken);
}
