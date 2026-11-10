using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Dashboard;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees;

public interface IEmployeeRepository
{
    Task<PageResult<EmployeeDetailsData>> SearchAccessibleAsync(string? search, Guid? departmentId, EmploymentStatus? status, string? cursor, int limit, CancellationToken cancellationToken);
    Task<Employee?> GetAccessibleAsync(Guid id, CancellationToken cancellationToken);
    Task<EmployeeDetailsData> GetDetailsAsync(Employee employee, CancellationToken cancellationToken);
    Task<EmployeeProfileDto?> GetProfileAccessibleAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmploymentHistoryDto>> GetEmploymentHistoryAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PositionAssignmentHistoryDto>> GetAssignmentHistoryAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportingLineHistoryDto>> GetReportingLineHistoryAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeReferenceDto>> GetDirectReportsAsync(Guid managerEmployeeId, CancellationToken cancellationToken);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAccessibleAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetAccessibleIdsAsync(CancellationToken cancellationToken);
    void Add(Employee employee);
    Task<Employment?> GetOpenEmploymentAsync(Guid employeeId, CancellationToken cancellationToken);
    void AddEmployment(Employment employment);
    Task<bool> HasOverlappingAssignmentAsync(Guid employeeId, DateOnly from, DateOnly? to, Guid? excludingAssignmentId, CancellationToken cancellationToken);
    Task<PositionAssignment?> GetAssignmentOnDateAsync(Guid employeeId, DateOnly onDate, CancellationToken cancellationToken);
    void AddAssignment(PositionAssignment assignment);
    Task<ReportingLine?> GetReportingLineOnDateAsync(Guid employeeId, DateOnly onDate, CancellationToken cancellationToken);
    Task<bool> HasOverlappingReportingLineAsync(Guid employeeId, DateOnly from, DateOnly? to, Guid? excludingReportingLineId, CancellationToken cancellationToken);
    void AddReportingLine(ReportingLine reportingLine);
}
