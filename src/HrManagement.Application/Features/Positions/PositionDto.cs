namespace HrManagement.Application.Features.Positions;

public sealed record PositionDto(Guid Id, Guid? DepartmentId, string Title, string? Code, long Version);
