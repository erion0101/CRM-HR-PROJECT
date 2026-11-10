using HrManagement.Domain;

namespace HrManagement.Application.Features.Dashboard;

public sealed record DashboardEmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string WorkEmail,
    EmploymentStatus? Status,
    string? Position,
    string? OrganizationUnit,
    DateOnly? HireDate);

public sealed record DashboardCelebrationDto(
    Guid EmployeeId,
    string FullName,
    string Type,
    DateOnly Date,
    int? Years);

public sealed record DashboardSummaryDto(
    int VisibleEmployees,
    int ActiveEmployees,
    int OnLeaveEmployees,
    int SetupRequiredEmployees,
    int ActiveOrganizationUnits,
    IReadOnlyList<DashboardEmployeeDto> RecentEmployees,
    IReadOnlyList<DashboardCelebrationDto> UpcomingCelebrations,
    IReadOnlyList<DashboardEmployeeDto> PeopleOnLeave);
