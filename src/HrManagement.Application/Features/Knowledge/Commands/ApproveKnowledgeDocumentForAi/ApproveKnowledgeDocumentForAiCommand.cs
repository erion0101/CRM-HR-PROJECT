using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.ApproveKnowledgeDocumentForAi;

public sealed record ApproveKnowledgeDocumentForAiCommand(Guid ArticleId, Guid DocumentId, DataClassification Classification)
    : ICommand<DocumentAiStatusDto>;
