using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees;

public sealed record EmployeeDto(Guid Id, string EmployeeNumber, string FirstName, string LastName, string WorkEmail, string? Phone, DateOnly? DateOfBirth, EmploymentStatus? Status, Guid? DepartmentId, Guid? PositionId, Guid? ManagerId, long Version);
