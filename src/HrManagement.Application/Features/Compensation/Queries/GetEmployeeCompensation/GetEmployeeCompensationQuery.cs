using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Compensation.Queries.GetEmployeeCompensation;

public sealed record GetEmployeeCompensationQuery(Guid EmployeeId) : IQuery<IReadOnlyList<CompensationDto>>;
