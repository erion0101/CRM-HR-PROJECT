using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Documents.Queries.GetDocumentFolders;

public sealed record GetDocumentFoldersQuery : IQuery<IReadOnlyList<DocumentFolderDto>>;
