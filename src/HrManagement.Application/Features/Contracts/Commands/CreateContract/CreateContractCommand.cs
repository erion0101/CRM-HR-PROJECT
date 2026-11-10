using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Contracts.Commands.CreateContract;

public sealed record CreateContractCommand(Guid EmployeeId, string ContractNumber, DateOnly StartDate, DateOnly? EndDate, string? Notes) : ICommand<ContractDto>;
