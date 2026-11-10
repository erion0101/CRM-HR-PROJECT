using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents;

public sealed record DocumentWithVersion(EmployeeDocument Document, DocumentVersion Version);
