using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.ReprocessDocumentIndex;

public sealed record ReprocessDocumentIndexCommand(Guid EmployeeId, Guid DocumentId) : ICommand<DocumentAiStatusDto>;
