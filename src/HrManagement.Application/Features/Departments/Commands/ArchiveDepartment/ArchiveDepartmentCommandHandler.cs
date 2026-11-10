using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Departments.Commands.ArchiveDepartment;

public sealed class ArchiveDepartmentCommandHandler(IDepartmentRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<ArchiveDepartmentCommand>
{
    public async Task Handle(ArchiveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var entity = await repository.GetAsync(organizationId, request.Id, cancellationToken) ?? throw new NotFoundException("Department not found.");
        if (entity.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        if (await repository.HasChildrenAsync(organizationId, request.Id, cancellationToken))
            throw new ConflictException("Move or archive child organization units before archiving this unit.");
        if (await repository.HasAssignmentsAsync(organizationId, request.Id, cancellationToken))
            throw new ConflictException("This organization unit has employee assignment history and cannot be archived.");
        entity.Archive(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
