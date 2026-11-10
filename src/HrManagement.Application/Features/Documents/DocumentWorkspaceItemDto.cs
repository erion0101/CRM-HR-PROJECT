using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents;

public sealed record DocumentWorkspaceItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid? FolderId,
    string DisplayName,
    string DocumentType,
    string ContentType,
    long SizeBytes,
    DocumentStatus Status,
    int CurrentVersion,
    DateTimeOffset UploadedAtUtc,
    long Version);
