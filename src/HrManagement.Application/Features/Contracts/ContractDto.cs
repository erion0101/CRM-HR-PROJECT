using HrManagement.Domain;

namespace HrManagement.Application.Features.Contracts;

public sealed record ContractDto(Guid Id, Guid EmployeeId, string ContractNumber, ContractStatus Status, DateOnly StartDate, DateOnly? EndDate, int Revision, long Version);
