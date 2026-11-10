namespace HrManagement.Application.Features.Projects;

public sealed record ProjectMemberDto(
    Guid MembershipId,
    Guid EmployeeId,
    string EmployeeName,
    string ProjectRole,
    string Responsibilities,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    long Version);
