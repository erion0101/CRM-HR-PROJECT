using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents.Commands.CreateDocumentFolder;

public sealed record CreateDocumentFolderCommand(
    string Name,
    string? Description,
    DocumentFolderScope Scope,
    Guid? EmployeeId,
    IReadOnlyList<string> AllowedRoles) : ICommand<DocumentFolderDto>;
