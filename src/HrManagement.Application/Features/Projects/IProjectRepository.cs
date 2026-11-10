using HrManagement.Domain;

namespace HrManagement.Application.Features.Projects;

public interface IProjectRepository
{
    void Add(Project project);
    void Add(ProjectMembership membership);
    void Add(ProjectResourceLink link);
    Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectMembership?> GetMembershipAsync(Guid projectId, Guid membershipId, CancellationToken cancellationToken);
    Task<bool> HasOverlappingMembershipAsync(Guid projectId, Guid employeeId, DateOnly from, DateOnly? to, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> CanAccessAsync(Guid projectId, CancellationToken cancellationToken);
    Task<bool> CanManageAsync(Guid projectId, CancellationToken cancellationToken);
    Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken);
    Task<bool> EmployeeHasActiveMembershipAsync(Guid projectId, Guid employeeId, DateOnly onDate, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectDto>> ListVisibleAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectEmployeeOptionDto>> ListEmployeeOptionsAsync(CancellationToken cancellationToken);
    Task<ProjectDto?> GetVisibleAsync(Guid id, CancellationToken cancellationToken);
}
