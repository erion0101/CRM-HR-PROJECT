using FluentValidation;
using HrManagement.Application.Common.Authorization;

namespace HrManagement.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(12).MaximumLength(256);
        RuleFor(x => x.Role).Must(x => Roles.All.Contains(x, StringComparer.Ordinal)).WithMessage("Unknown role.");
    }
}
