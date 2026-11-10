using HrManagement.Application.Common.Models;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments;

public interface IDepartmentRepository
{
    Task<PageResult<DepartmentDetailsData>> ListAsync(Guid organizationId, string? cursor, int limit, CancellationToken cancellationToken);
    Task<DepartmentOverviewDto?> GetOverviewAsync(Guid organizationId, Guid? departmentId, CancellationToken cancellationToken);
    Task<Department?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
    void Add(Department department);
    Task<bool> ExistsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
    Task<bool> WouldCreateCycleAsync(Guid organizationId, Guid departmentId, Guid parentDepartmentId, CancellationToken cancellationToken);
    Task<bool> HasIncompatibleChildrenAsync(Guid organizationId, Guid departmentId, OrganizationUnitType type, CancellationToken cancellationToken);
    Task<bool> HasChildrenAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken);
    Task<bool> HasAssignmentsAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken);
    Task<int> CountActiveDepartmentsAsync(Guid organizationId, CancellationToken cancellationToken);
}
