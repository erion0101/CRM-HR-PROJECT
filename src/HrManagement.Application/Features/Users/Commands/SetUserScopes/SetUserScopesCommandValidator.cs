using FluentValidation;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Users.Commands.SetUserScopes;

public sealed class SetUserScopesCommandValidator : AbstractValidator<SetUserScopesCommand>
{
    public SetUserScopesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Scopes).NotNull().Must(x => x.Count <= 50).WithMessage("No more than 50 scope assignments are allowed.");
        RuleForEach(x => x.Scopes).Must(scope => scope.ScopeType == ScopeType.Department ? scope.ScopeEntityId.HasValue : scope.ScopeEntityId is null)
            .WithMessage("Only department scopes require a scope entity identifier.");
    }
}
