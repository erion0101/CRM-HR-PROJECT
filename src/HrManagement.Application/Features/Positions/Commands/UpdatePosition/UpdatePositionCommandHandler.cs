using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Departments;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Positions.Commands.UpdatePosition;

public sealed class UpdatePositionCommandHandler(IPositionRepository repository, IDepartmentRepository departments, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<UpdatePositionCommand, PositionDto>
{
    public async Task<PositionDto> Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var entity = await repository.GetAsync(organizationId, request.Id, cancellationToken) ?? throw new NotFoundException("Position not found.");
        if (entity.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        var department = await departments.GetAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]> { ["departmentId"] = ["Department does not exist."] });
        if (department.Type != OrganizationUnitType.Department)
            throw new ValidationException(new Dictionary<string, string[]> { ["departmentId"] = ["Positions can only belong to an IT or HR department."] });
        entity.Update(department.Id, request.Title, request.Code, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PositionDto(entity.Id, entity.DepartmentId, entity.Title, entity.Code, entity.Version);
    }
}
