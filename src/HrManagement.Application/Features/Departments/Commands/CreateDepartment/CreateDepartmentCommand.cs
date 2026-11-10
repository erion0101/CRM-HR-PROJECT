using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments.Commands.CreateDepartment;

public sealed record CreateDepartmentCommand(
    string Name,
    string? Code,
    OrganizationUnitType Type = OrganizationUnitType.Department,
    Guid? ParentDepartmentId = null,
    Guid? ManagerEmployeeId = null,
    string? Description = null,
    Guid? HrEmployeeId = null,
    Guid? ProductManagerEmployeeId = null) : ICommand<DepartmentDto>;
