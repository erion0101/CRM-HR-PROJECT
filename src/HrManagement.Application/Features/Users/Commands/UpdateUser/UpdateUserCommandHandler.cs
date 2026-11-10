using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IIdentityService identity) : ICommandHandler<UpdateUserCommand, UserDto>
{
    public Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken) => identity.UpdateAsync(request.Id, request.IsActive, request.Role, request.EmployeeId, cancellationToken);
}
