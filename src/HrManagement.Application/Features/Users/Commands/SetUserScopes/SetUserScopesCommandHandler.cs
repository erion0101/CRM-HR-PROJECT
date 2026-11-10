using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Users.Commands.SetUserScopes;

public sealed class SetUserScopesCommandHandler(IIdentityService identity) : ICommandHandler<SetUserScopesCommand>
{
    public Task Handle(SetUserScopesCommand request, CancellationToken cancellationToken) => identity.SetScopesAsync(request.UserId, request.Scopes, cancellationToken);
}
