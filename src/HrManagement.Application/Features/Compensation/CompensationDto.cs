namespace HrManagement.Application.Features.Compensation;

public sealed record CompensationDto(Guid Id, Guid EmployeeId, decimal Amount, string Currency, string Frequency, DateOnly EffectiveFrom, DateOnly? EffectiveTo, long Version);
