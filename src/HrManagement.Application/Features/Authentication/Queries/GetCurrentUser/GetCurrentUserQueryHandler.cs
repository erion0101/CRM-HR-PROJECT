using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Users;

namespace HrManagement.Application.Features.Authentication.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IIdentityService identity) : IQueryHandler<GetCurrentUserQuery, CurrentUser?>
{
    public Task<CurrentUser?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) => identity.GetCurrentAsync(cancellationToken);
}
