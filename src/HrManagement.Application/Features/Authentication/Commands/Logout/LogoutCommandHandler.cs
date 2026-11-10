using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Users;

namespace HrManagement.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutCommandHandler(IIdentityService identity) : ICommandHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken) => identity.SignOutAsync();
}
