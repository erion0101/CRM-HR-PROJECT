using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Documents.Queries.GetEmployeeDocuments;

public sealed record GetEmployeeDocumentsQuery(Guid EmployeeId) : IQuery<IReadOnlyList<DocumentDto>>;
