using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees;

public sealed record OrganizationReferenceDto(Guid Id, string Name, string? Code);

public sealed record EmployeeReferenceDto(Guid Id, string EmployeeNumber, string FullName, string WorkEmail);

public sealed record EmployeeProfileDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string WorkEmail,
    string? Phone,
    DateOnly? DateOfBirth,
    EmploymentStatus? Status,
    DateOnly? HireDate,
    DateOnly? EmploymentEndDate,
    OrganizationReferenceDto? Department,
    IReadOnlyList<OrganizationReferenceDto> OrganizationPath,
    OrganizationReferenceDto? Position,
    EmployeeReferenceDto? Manager,
    IReadOnlyList<EmployeeReferenceDto> DirectReports,
    long Version);

public sealed record EmploymentHistoryDto(Guid Id, EmploymentStatus Status, DateOnly StartDate, DateOnly? EndDate, long Version);

public sealed record PositionAssignmentHistoryDto(
    Guid Id,
    OrganizationReferenceDto Position,
    OrganizationReferenceDto Department,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    long Version);

public sealed record ReportingLineHistoryDto(
    Guid Id,
    EmployeeReferenceDto Manager,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    long Version);
