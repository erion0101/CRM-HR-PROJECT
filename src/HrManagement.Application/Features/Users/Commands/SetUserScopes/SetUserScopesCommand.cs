using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Users.Commands.SetUserScopes;

public sealed record SetUserScopesCommand(Guid UserId, IReadOnlyList<UserScopeInput> Scopes) : ICommand;
