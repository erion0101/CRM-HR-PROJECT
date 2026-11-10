namespace HrManagement.Application.Features.Employees;

public static class EmployeeMappings
{
    public static EmployeeDto ToDto(this EmployeeDetailsData data) => new(
        data.Employee.Id,
        data.Employee.EmployeeNumber,
        data.Employee.FirstName,
        data.Employee.LastName,
        data.Employee.WorkEmail,
        data.Employee.Phone,
        data.Employee.DateOfBirth,
        data.Employment?.Status,
        data.Assignment?.DepartmentId,
        data.Assignment?.PositionId,
        data.ReportingLine?.ManagerEmployeeId,
        data.Employee.Version);
}
