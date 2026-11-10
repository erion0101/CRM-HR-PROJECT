using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Compensation.Commands.AddCompensation;

public sealed record AddCompensationCommand(Guid EmployeeId, decimal Amount, string Currency, string Frequency, DateOnly EffectiveFrom, DateOnly? EffectiveTo) : ICommand<CompensationDto>;
