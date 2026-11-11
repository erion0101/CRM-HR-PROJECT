using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IIdentityService identity) : IQueryHandler<GetUsersQuery, PageResult<UserDto>>
{
    public Task<PageResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken) => identity.ListAsync(request.Cursor, request.Limit, request.UnlinkedOnly, cancellationToken);
}
