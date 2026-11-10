using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Documents;

namespace HrManagement.Application.Features.Knowledge.Commands.UploadKnowledgeDocument;

public sealed record UploadKnowledgeDocumentCommand(Guid ArticleId, DocumentUpload Upload) : ICommand<DocumentDto>;
