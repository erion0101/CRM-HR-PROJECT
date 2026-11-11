using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Queries.LeaveApprovals;

public sealed record GetLeaveApprovalsQuery : IQuery<IReadOnlyList<LeaveApprovalDto>>;

/// <summary>
/// Pending requests the caller can decide: a manager sees their current direct reports, HR sees everyone.
/// Each row shows the balance after the request and which teammates are already off on those days.
/// </summary>
public sealed class GetLeaveApprovalsQueryHandler(ILeaveRepository repository, LeaveService leave, ICurrentUserService currentUser)
    : IQueryHandler<GetLeaveApprovalsQuery, IReadOnlyList<LeaveApprovalDto>>
{
    public async Task<IReadOnlyList<LeaveApprovalDto>> Handle(GetLeaveApprovalsQuery request, CancellationToken cancellationToken)
    {
        var today = LeaveService.Today;
        IReadOnlyList<Guid>? reports = null;
        if (!leave.IsHr)
        {
            if (currentUser.EmployeeId is not { } self) return [];
            reports = await repository.GetDirectReportsAsync(self, today, cancellationToken);
            if (reports.Count == 0) return [];
        }
        var pending = (await repository.ListPendingAsync(reports, cancellationToken))
            .Where(x => x.EmployeeId != currentUser.EmployeeId).OrderBy(x => x.StartDate).ToArray();
        if (pending.Length == 0) return [];

        var dtos = await leave.ToDtosAsync(pending, cancellationToken);
        var result = new List<LeaveApprovalDto>(pending.Length);
        for (var i = 0; i < pending.Length; i++)
        {
            var item = pending[i];
            var context = await leave.LoadAsync(item.EmployeeId, cancellationToken);
            var type = context.Types.Single(x => x.Id == item.LeaveTypeId);
            decimal? after = type.DeductsFromBalance
                ? context.BalanceFor(type, item.StartDate > today ? item.StartDate : today.Year == item.StartDate.Year ? today : new DateOnly(item.StartDate.Year, 12, 31)).Available
                : null;

            var manager = await repository.GetCurrentManagerAsync(item.EmployeeId, today, cancellationToken);
            var team = manager is { } m ? await repository.GetDirectReportsAsync(m, today, cancellationToken) : [];
            var teammates = team.Where(x => x != item.EmployeeId).ToArray();
            var overlapping = teammates.Length == 0 ? [] : await repository.ListActiveOverlappingAsync(teammates, item.StartDate, item.EndDate, cancellationToken);
            var names = await repository.GetEmployeeNamesAsync(overlapping.Select(x => x.EmployeeId), cancellationToken);
            result.Add(new LeaveApprovalDto(dtos[i], after, overlapping.Select(x => x.EmployeeId).Distinct()
                .Select(x => names.GetValueOrDefault(x, "A colleague")).Order().ToArray()));
        }
        return result;
    }
}
