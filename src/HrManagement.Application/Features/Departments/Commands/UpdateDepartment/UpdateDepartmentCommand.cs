using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments.Commands.UpdateDepartment;

public sealed record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    string? Code,
    OrganizationUnitType Type,
    Guid? ParentDepartmentId,
    Guid? ManagerEmployeeId,
    Guid? HrEmployeeId,
    Guid? ProductManagerEmployeeId,
    string? Description,
    long ExpectedVersion) : ICommand<DepartmentDto>;
