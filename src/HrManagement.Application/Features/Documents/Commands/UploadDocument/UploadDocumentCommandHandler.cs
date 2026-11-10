using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents.Commands.UploadDocument;

public sealed class UploadDocumentCommandHandler(IDocumentRepository documents, IEmployeeRepository employees, IDocumentStorageService storage, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var canManageDocuments = currentUser.IsInRole(Roles.Hr);
        if (!canManageDocuments && currentUser.EmployeeId != request.EmployeeId) throw new ForbiddenException();
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        if (request.FolderId is { } folderId)
        {
            var folder = await documents.GetFolderAsync(folderId, cancellationToken);
            if (folder is null || (folder.Scope == DocumentFolderScope.Employee && folder.EmployeeId != request.EmployeeId))
                throw new NotFoundException("Document folder not found.");
        }
        var document = EmployeeDocument.Create(organizationId, request.Upload.FileName, request.Upload.DocumentType, request.FolderId);
        var key = await storage.SaveQuarantineAsync(request.Upload.Content, cancellationToken);
        var version = DocumentVersion.Create(organizationId, document.Id, 1, request.Upload.FileName, key, DocumentContentTypes.FromFileName(request.Upload.FileName), request.Upload.Length);
        documents.Add(document);
        documents.AddVersion(version);
        documents.AddAssociation(DocumentAssociation.Create(organizationId, document.Id, DocumentAssociationType.Employee, request.EmployeeId));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return document.ToDto();
    }
}
