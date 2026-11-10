using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Contracts.Queries.GetEmployeeContracts;

public sealed record GetEmployeeContractsQuery(Guid EmployeeId) : IQuery<IReadOnlyList<ContractDto>>;
