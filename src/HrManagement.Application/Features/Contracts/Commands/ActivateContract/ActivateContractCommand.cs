using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Contracts.Commands.ActivateContract;

public sealed record ActivateContractCommand(Guid EmployeeId, Guid ContractId, long ExpectedVersion) : ICommand<ContractDto>;
