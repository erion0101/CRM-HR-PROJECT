using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents;

public sealed record DocumentDto(Guid Id, Guid? FolderId, string DisplayName, string DocumentType, DocumentStatus Status, int CurrentVersion, long Version);
