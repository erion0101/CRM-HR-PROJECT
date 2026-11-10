using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string? Code,
    OrganizationUnitType Type,
    Guid? ParentDepartmentId,
    Guid? ManagerEmployeeId,
    string? ManagerName,
    Guid? HrEmployeeId,
    string? HrName,
    Guid? ProductManagerEmployeeId,
    string? ProductManagerName,
    string? Description,
    int DirectEmployeeCount,
    int TotalEmployeeCount,
    int ChildUnitCount,
    long Version);

public sealed record DepartmentEmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string WorkEmail,
    string? Position,
    EmploymentStatus? EmploymentStatus,
    IReadOnlyList<string> Projects);

public sealed record DepartmentOverviewDto(
    DepartmentDto Department,
    IReadOnlyList<DepartmentEmployeeDto> Employees);
