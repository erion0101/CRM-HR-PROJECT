using FluentValidation;

namespace HrManagement.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(1_000);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.LeadEmployeeId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum().NotEqual(Domain.ProjectStatus.Archived);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
