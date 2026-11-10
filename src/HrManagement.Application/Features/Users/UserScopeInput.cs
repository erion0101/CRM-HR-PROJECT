using HrManagement.Domain;

namespace HrManagement.Application.Features.Users;

public sealed record UserScopeInput(ScopeType ScopeType, Guid? ScopeEntityId);
