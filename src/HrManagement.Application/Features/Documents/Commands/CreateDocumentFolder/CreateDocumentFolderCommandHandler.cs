using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents.Commands.CreateDocumentFolder;

public sealed class CreateDocumentFolderCommandHandler(
    IDocumentRepository documents,
    IEmployeeRepository employees,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateDocumentFolderCommand, DocumentFolderDto>
{
    public async Task<DocumentFolderDto> Handle(CreateDocumentFolderCommand request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId is { } employeeId && !await employees.ExistsAccessibleAsync(employeeId, cancellationToken))
            throw new NotFoundException("Employee not found.");
        if (await documents.FolderNameExistsAsync(request.Name, request.EmployeeId, cancellationToken))
            throw new ConflictException("A document folder with this name already exists.");

        var folder = DocumentFolder.Create(
            currentUser.OrganizationId ?? throw new ForbiddenException(),
            request.Name,
            request.Description,
            request.Scope,
            request.EmployeeId,
            request.AllowedRoles);
        documents.AddFolder(folder);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DocumentFolderDto(folder.Id, folder.Name, folder.Description, folder.IsSystemFolder, folder.Scope, folder.EmployeeId, folder.AllowedRoles, 0, folder.Version);
    }
}
