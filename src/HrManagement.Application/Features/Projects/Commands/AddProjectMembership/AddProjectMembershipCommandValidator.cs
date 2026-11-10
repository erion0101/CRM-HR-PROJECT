using FluentValidation;

namespace HrManagement.Application.Features.Projects.Commands.AddProjectMembership;

public sealed class AddProjectMembershipCommandValidator : AbstractValidator<AddProjectMembershipCommand>
{
    public AddProjectMembershipCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ProjectRole).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Responsibilities).NotEmpty().MaximumLength(1_000);
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue);
    }
}
