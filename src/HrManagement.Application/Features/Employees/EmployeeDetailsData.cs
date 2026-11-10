using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees;

public sealed record EmployeeDetailsData(Employee Employee, Employment? Employment, PositionAssignment? Assignment, ReportingLine? ReportingLine);
