using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Positions.Commands.ArchivePosition;

public sealed class ArchivePositionCommandHandler(IPositionRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<ArchivePositionCommand>
{
    public async Task Handle(ArchivePositionCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var entity = await repository.GetAsync(organizationId, request.Id, cancellationToken) ?? throw new NotFoundException("Position not found.");
        if (entity.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        entity.Archive(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
