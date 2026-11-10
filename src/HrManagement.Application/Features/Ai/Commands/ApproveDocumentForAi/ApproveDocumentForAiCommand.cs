using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;

public sealed record ApproveDocumentForAiCommand(Guid EmployeeId, Guid DocumentId, DataClassification Classification) : ICommand<DocumentAiStatusDto>;
