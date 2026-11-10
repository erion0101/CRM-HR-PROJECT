using FluentValidation;
using HrManagement.Application.Common.Authorization;

namespace HrManagement.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Role).Must(x => Roles.All.Contains(x, StringComparer.Ordinal)).WithMessage("Unknown role.");
    }
}
