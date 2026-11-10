using FluentValidation;

namespace HrManagement.Application.Features.Onboarding.Commands.AssignOnboarding;

public sealed class AssignOnboardingCommandValidator : AbstractValidator<AssignOnboardingCommand>
{
    public AssignOnboardingCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.MentorEmployeeId).NotEqual(x => x.EmployeeId).When(x => x.MentorEmployeeId.HasValue);
    }
}
