using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents.Commands.UploadDocumentVersion;

public sealed class UploadDocumentVersionCommandHandler(IDocumentRepository documents, IEmployeeRepository employees, IDocumentStorageService storage, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<UploadDocumentVersionCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var document = await documents.GetAsync(request.EmployeeId, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        if (document.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        var key = await storage.SaveQuarantineAsync(request.Upload.Content, cancellationToken);
        document.AddVersion(DateTimeOffset.UtcNow);
        documents.AddVersion(DocumentVersion.Create(currentUser.OrganizationId ?? throw new ForbiddenException(), document.Id, document.CurrentVersion, request.Upload.FileName, key, DocumentContentTypes.FromFileName(request.Upload.FileName), request.Upload.Length));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return document.ToDto();
    }
}
