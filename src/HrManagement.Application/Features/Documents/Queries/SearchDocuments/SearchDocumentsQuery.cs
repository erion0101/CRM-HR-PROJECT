using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Documents.Queries.SearchDocuments;

public sealed record SearchDocumentsQuery(Guid? FolderId, string? Search) : IQuery<IReadOnlyList<DocumentWorkspaceItemDto>>;
