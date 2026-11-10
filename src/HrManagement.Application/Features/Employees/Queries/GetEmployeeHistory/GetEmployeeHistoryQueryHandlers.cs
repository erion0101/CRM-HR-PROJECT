using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployeeHistory;

public sealed class GetEmploymentHistoryQueryHandler(IEmployeeRepository repository) : IQueryHandler<GetEmploymentHistoryQuery, IReadOnlyList<EmploymentHistoryDto>>
{
    public async Task<IReadOnlyList<EmploymentHistoryDto>> Handle(GetEmploymentHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return await repository.GetEmploymentHistoryAsync(request.EmployeeId, cancellationToken);
    }
}

public sealed class GetAssignmentHistoryQueryHandler(IEmployeeRepository repository) : IQueryHandler<GetAssignmentHistoryQuery, IReadOnlyList<PositionAssignmentHistoryDto>>
{
    public async Task<IReadOnlyList<PositionAssignmentHistoryDto>> Handle(GetAssignmentHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return await repository.GetAssignmentHistoryAsync(request.EmployeeId, cancellationToken);
    }
}

public sealed class GetReportingLineHistoryQueryHandler(IEmployeeRepository repository) : IQueryHandler<GetReportingLineHistoryQuery, IReadOnlyList<ReportingLineHistoryDto>>
{
    public async Task<IReadOnlyList<ReportingLineHistoryDto>> Handle(GetReportingLineHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return await repository.GetReportingLineHistoryAsync(request.EmployeeId, cancellationToken);
    }
}

public sealed class GetDirectReportsQueryHandler(IEmployeeRepository repository) : IQueryHandler<GetDirectReportsQuery, IReadOnlyList<EmployeeReferenceDto>>
{
    public async Task<IReadOnlyList<EmployeeReferenceDto>> Handle(GetDirectReportsQuery request, CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return await repository.GetDirectReportsAsync(request.EmployeeId, cancellationToken);
    }
}
