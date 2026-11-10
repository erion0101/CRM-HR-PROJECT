using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetDocumentAiStatus;

public sealed record GetDocumentAiStatusQuery(Guid EmployeeId, Guid DocumentId) : IQuery<DocumentAiStatusDto>;
