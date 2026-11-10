using FluentValidation;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateReadingProgress;

public sealed class UpdateReadingProgressCommandValidator : AbstractValidator<UpdateReadingProgressCommand>
{
    public UpdateReadingProgressCommandValidator()
    {
        RuleFor(x => x.Status).Must(x => x is OnboardingReadingStatus.InProgress or OnboardingReadingStatus.Read)
            .WithMessage("Reading can only be started or marked as read.");
    }
}
