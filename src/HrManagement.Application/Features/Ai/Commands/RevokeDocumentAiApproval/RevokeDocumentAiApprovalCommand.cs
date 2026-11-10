using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.RevokeDocumentAiApproval;

public sealed record RevokeDocumentAiApprovalCommand(Guid EmployeeId, Guid DocumentId) : ICommand;
