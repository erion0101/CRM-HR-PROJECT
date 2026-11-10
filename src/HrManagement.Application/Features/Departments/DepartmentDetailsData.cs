using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments;

public sealed record DepartmentDetailsData(
    Department Department,
    string? ManagerName,
    string? HrName,
    string? ProductManagerName,
    int DirectEmployeeCount,
    int TotalEmployeeCount,
    int ChildUnitCount);
