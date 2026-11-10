using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentCommandHandler(IDepartmentRepository repository, HrManagement.Application.Features.Employees.IEmployeeRepository employees, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<CreateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        if (request.Type == OrganizationUnitType.Department && await repository.CountActiveDepartmentsAsync(organizationId, cancellationToken) >= 2)
            throw new ConflictException("This company already has its two active departments: IT and HR.");
        await ValidateParentAsync(organizationId, request.Type, request.ParentDepartmentId, cancellationToken);
        await ValidateLeadershipAsync(request.ManagerEmployeeId, request.HrEmployeeId, request.ProductManagerEmployeeId, cancellationToken);
        var entity = Department.Create(organizationId, request.Name, request.Code, request.Type, request.ParentDepartmentId,
            request.ManagerEmployeeId, request.Description, request.HrEmployeeId, request.ProductManagerEmployeeId);
        repository.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DepartmentDto(entity.Id, entity.Name, entity.Code, entity.Type, entity.ParentDepartmentId,
            entity.ManagerEmployeeId, null, entity.HrEmployeeId, null, entity.ProductManagerEmployeeId, null,
            entity.Description, 0, 0, 0, entity.Version);
    }

    private async Task ValidateParentAsync(Guid organizationId, OrganizationUnitType type, Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is not { } id) return;
        var parent = await repository.GetAsync(organizationId, id, cancellationToken) ?? throw new NotFoundException("Parent organization unit not found.");
        if (!Department.CanContain(parent.Type, type))
            throw new DomainException($"A {parent.Type} cannot contain a {type}.");
    }

    private async Task ValidateLeadershipAsync(Guid? managerId, Guid? hrId, Guid? productManagerId, CancellationToken cancellationToken)
    {
        foreach (var employeeId in new[] { managerId, hrId, productManagerId }.OfType<Guid>().Distinct())
            if (!await employees.ExistsAccessibleAsync(employeeId, cancellationToken))
                throw new NotFoundException("Leadership employee not found.");
    }
}
