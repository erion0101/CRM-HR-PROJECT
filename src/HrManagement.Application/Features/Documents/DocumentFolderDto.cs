using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents;

public sealed record DocumentFolderDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemFolder,
    DocumentFolderScope Scope,
    Guid? EmployeeId,
    IReadOnlyList<string> AllowedRoles,
    int ItemCount,
    long Version);
