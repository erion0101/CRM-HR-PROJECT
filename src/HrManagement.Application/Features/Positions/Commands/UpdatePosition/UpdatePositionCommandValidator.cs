using FluentValidation;

namespace HrManagement.Application.Features.Positions.Commands.UpdatePosition;

public sealed class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
