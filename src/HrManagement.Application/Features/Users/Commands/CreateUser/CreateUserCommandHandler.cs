using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(IIdentityService identity) : ICommandHandler<CreateUserCommand, UserDto>
{
    public Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken) => identity.CreateAsync(request.Email, request.TemporaryPassword, request.Role, request.EmployeeId, cancellationToken);
}
