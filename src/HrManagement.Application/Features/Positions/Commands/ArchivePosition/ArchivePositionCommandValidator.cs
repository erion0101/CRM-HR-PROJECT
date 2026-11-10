using FluentValidation;

namespace HrManagement.Application.Features.Positions.Commands.ArchivePosition;

public sealed class ArchivePositionCommandValidator : AbstractValidator<ArchivePositionCommand>
{
    public ArchivePositionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
