using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Leave.Queries.LeaveSettings;

public sealed record GetLeaveTypesQuery(bool IncludeArchived = false) : IQuery<IReadOnlyList<LeaveTypeDto>>;
public sealed record GetPublicHolidaysQuery : IQuery<IReadOnlyList<PublicHolidayDto>>;

public sealed class GetLeaveTypesQueryHandler(ILeaveRepository repository) : IQueryHandler<GetLeaveTypesQuery, IReadOnlyList<LeaveTypeDto>>
{
    public async Task<IReadOnlyList<LeaveTypeDto>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken) =>
        (await repository.ListTypesAsync(request.IncludeArchived, cancellationToken)).OrderBy(x => x.IsDeleted).ThenBy(x => x.CreatedAtUtc).ThenBy(x => x.Name).Select(x => x.ToDto()).ToArray();
}

public sealed class GetPublicHolidaysQueryHandler(ILeaveRepository repository) : IQueryHandler<GetPublicHolidaysQuery, IReadOnlyList<PublicHolidayDto>>
{
    public async Task<IReadOnlyList<PublicHolidayDto>> Handle(GetPublicHolidaysQuery request, CancellationToken cancellationToken) =>
        (await repository.ListHolidaysAsync(cancellationToken)).OrderBy(x => x.Date.Month).ThenBy(x => x.Date.Day).Select(x => x.ToDto()).ToArray();
}
