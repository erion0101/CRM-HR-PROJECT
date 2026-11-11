using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Users.Queries.GetEmployeeLogin;

/// <summary>The login for one employee, shown on their profile. <see cref="Login"/> is null when they have none yet.</summary>
public sealed record EmployeeLoginDto(UserDto? Login);

public sealed record GetEmployeeLoginQuery(Guid EmployeeId) : IQuery<EmployeeLoginDto>;

public sealed class GetEmployeeLoginQueryHandler(IIdentityService identity, IAuthorizedDataScope scope)
    : IQueryHandler<GetEmployeeLoginQuery, EmployeeLoginDto>
{
    public async Task<EmployeeLoginDto> Handle(GetEmployeeLoginQuery request, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return new EmployeeLoginDto(await identity.GetByEmployeeAsync(request.EmployeeId, cancellationToken));
    }
}
