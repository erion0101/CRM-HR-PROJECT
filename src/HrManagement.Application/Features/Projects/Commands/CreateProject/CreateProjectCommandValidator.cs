using FluentValidation;

namespace HrManagement.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(1_000);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.LeadEmployeeId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Links).NotNull().Must(x => x.Count <= 30).WithMessage("A project can have at most 30 resource links.");
        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(x => x.Label).NotEmpty().MaximumLength(120);
            link.RuleFor(x => x.Url).NotEmpty().MaximumLength(2_000).Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https");
            link.RuleFor(x => x.ResourceType).NotEmpty().MaximumLength(80);
        });
    }
}
