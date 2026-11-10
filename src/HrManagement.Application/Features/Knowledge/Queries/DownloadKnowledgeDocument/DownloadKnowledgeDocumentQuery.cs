using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Knowledge.Queries.DownloadKnowledgeDocument;

public sealed record DownloadKnowledgeDocumentQuery(Guid ArticleId, Guid DocumentId) : IQuery<DocumentDownload>;
