using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Documents;

namespace HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeDocuments;

public sealed record GetKnowledgeDocumentsQuery(Guid ArticleId) : IQuery<IReadOnlyList<DocumentDto>>;
