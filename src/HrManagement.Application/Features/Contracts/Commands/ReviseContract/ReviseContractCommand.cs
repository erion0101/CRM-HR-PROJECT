using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Contracts.Commands.ReviseContract;

public sealed record ReviseContractCommand(Guid EmployeeId, Guid ContractId, DateOnly StartDate, DateOnly? EndDate, string? Notes, long ExpectedVersion) : ICommand<ContractDto>;
