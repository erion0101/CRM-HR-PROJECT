using HrManagement.Domain;

namespace HrManagement.Application.Features.Contracts;

public static class ContractMappings
{
    public static ContractDto ToDto(this Contract contract) => new(contract.Id, contract.EmployeeId, contract.ContractNumber, contract.Status, contract.StartDate, contract.EndDate, contract.CurrentRevision, contract.Version);
}
