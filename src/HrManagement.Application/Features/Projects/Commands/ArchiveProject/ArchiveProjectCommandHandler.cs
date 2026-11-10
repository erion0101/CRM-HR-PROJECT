using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Commands.ArchiveProject;

public sealed class ArchiveProjectCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork) : ICommandHandler<ArchiveProjectCommand>
{
    public async Task Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Project not found.");
        if (!await repository.CanManageAsync(project.Id, cancellationToken)) throw new NotFoundException("Project not found.");
        if (project.Version != request.ExpectedVersion) throw new ConflictException("The project was changed by another user.");
        project.ArchiveProject(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
