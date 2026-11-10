using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Documents.Commands.ArchiveDocument;

public sealed class ArchiveDocumentCommandHandler(IDocumentRepository documents, IEmployeeRepository employees, IUnitOfWork unitOfWork)
    : ICommandHandler<ArchiveDocumentCommand>
{
    public async Task Handle(ArchiveDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var document = await documents.GetAsync(request.EmployeeId, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        if (document.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        document.Archive(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
