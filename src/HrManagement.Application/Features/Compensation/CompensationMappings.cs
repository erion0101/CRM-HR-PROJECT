using HrManagement.Domain;

namespace HrManagement.Application.Features.Compensation;

public static class CompensationMappings
{
    public static CompensationDto ToDto(this CompensationRecord record) => new(record.Id, record.EmployeeId, record.Amount, record.Currency, record.Frequency, record.EffectiveFrom, record.EffectiveTo, record.Version);
}
