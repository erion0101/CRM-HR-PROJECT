using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployeeHistory;

public sealed record GetEmploymentHistoryQuery(Guid EmployeeId) : IQuery<IReadOnlyList<EmploymentHistoryDto>>;
public sealed record GetAssignmentHistoryQuery(Guid EmployeeId) : IQuery<IReadOnlyList<PositionAssignmentHistoryDto>>;
public sealed record GetReportingLineHistoryQuery(Guid EmployeeId) : IQuery<IReadOnlyList<ReportingLineHistoryDto>>;
public sealed record GetDirectReportsQuery(Guid EmployeeId) : IQuery<IReadOnlyList<EmployeeReferenceDto>>;
