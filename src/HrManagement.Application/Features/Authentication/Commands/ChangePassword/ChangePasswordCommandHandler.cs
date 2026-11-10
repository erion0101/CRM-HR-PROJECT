using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Users;

namespace HrManagement.Application.Features.Authentication.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(IIdentityService identity) : ICommandHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken) => identity.ChangePasswordAsync(request.CurrentPassword, request.NewPassword, cancellationToken);
}
