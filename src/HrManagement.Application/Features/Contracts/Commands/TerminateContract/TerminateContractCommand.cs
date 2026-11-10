using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Contracts.Commands.TerminateContract;

public sealed record TerminateContractCommand(Guid EmployeeId, Guid ContractId, DateOnly TerminationDate, long ExpectedVersion) : ICommand<ContractDto>;
