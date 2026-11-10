using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Positions.Queries.GetPositions;

public sealed class GetPositionsQueryHandler(IPositionRepository repository, ICurrentUserService currentUser)
    : IQueryHandler<GetPositionsQuery, PageResult<PositionDto>>
{
    public async Task<PageResult<PositionDto>> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var page = await repository.ListAsync(organizationId, request.Cursor, request.Limit, cancellationToken);
        return new PageResult<PositionDto>(page.Items.Select(x => new PositionDto(x.Id, x.DepartmentId, x.Title, x.Code, x.Version)).ToArray(), page.NextCursor);
    }
}
