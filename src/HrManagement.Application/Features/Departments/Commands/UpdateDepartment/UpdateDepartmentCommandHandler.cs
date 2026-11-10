using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentCommandHandler(IDepartmentRepository repository, HrManagement.Application.Features.Employees.IEmployeeRepository employees, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<UpdateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var entity = await repository.GetAsync(organizationId, request.Id, cancellationToken) ?? throw new NotFoundException("Department not found.");
        if (entity.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        if (request.ParentDepartmentId is { } parentId)
        {
            var parent = await repository.GetAsync(organizationId, parentId, cancellationToken) ?? throw new NotFoundException("Parent organization unit not found.");
            if (!Department.CanContain(parent.Type, request.Type))
                throw new DomainException($"A {parent.Type} cannot contain a {request.Type}.");
            if (await repository.WouldCreateCycleAsync(organizationId, request.Id, parentId, cancellationToken))
                throw new DomainException("The selected parent would create an organization hierarchy cycle.");
        }
        if (await repository.HasIncompatibleChildrenAsync(organizationId, request.Id, request.Type, cancellationToken))
            throw new DomainException("The selected type is incompatible with one or more child organization units.");
        foreach (var employeeId in new[] { request.ManagerEmployeeId, request.HrEmployeeId, request.ProductManagerEmployeeId }.OfType<Guid>().Distinct())
            if (!await employees.ExistsAccessibleAsync(employeeId, cancellationToken))
                throw new NotFoundException("Leadership employee not found.");
        entity.Update(request.Name, request.Code, request.Type, request.ParentDepartmentId, request.ManagerEmployeeId,
            request.HrEmployeeId, request.ProductManagerEmployeeId, request.Description, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DepartmentDto(entity.Id, entity.Name, entity.Code, entity.Type, entity.ParentDepartmentId,
            entity.ManagerEmployeeId, null, entity.HrEmployeeId, null, entity.ProductManagerEmployeeId, null,
            entity.Description, 0, 0, 0, entity.Version);
    }
}
