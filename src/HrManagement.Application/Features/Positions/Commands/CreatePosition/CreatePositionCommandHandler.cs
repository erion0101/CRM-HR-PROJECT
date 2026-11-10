using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Departments;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Positions.Commands.CreatePosition;

public sealed class CreatePositionCommandHandler(IPositionRepository repository, IDepartmentRepository departments, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<CreatePositionCommand, PositionDto>
{
    public async Task<PositionDto> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var department = await departments.GetAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]> { ["departmentId"] = ["Department does not exist."] });
        if (department.Type != OrganizationUnitType.Department)
            throw new ValidationException(new Dictionary<string, string[]> { ["departmentId"] = ["Positions can only belong to an IT or HR department."] });
        var entity = Position.Create(organizationId, department.Id, request.Title, request.Code);
        repository.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PositionDto(entity.Id, entity.DepartmentId, entity.Title, entity.Code, entity.Version);
    }
}
