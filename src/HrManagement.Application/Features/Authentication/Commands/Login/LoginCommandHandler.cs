using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Users;

namespace HrManagement.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler(IIdentityService identity) : ICommandHandler<LoginCommand, bool>
{
    public Task<bool> Handle(LoginCommand request, CancellationToken cancellationToken) => identity.SignInAsync(request.Email, request.Password, request.RememberMe, cancellationToken);
}
