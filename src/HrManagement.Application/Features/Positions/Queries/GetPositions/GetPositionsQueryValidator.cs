using FluentValidation;

namespace HrManagement.Application.Features.Positions.Queries.GetPositions;

public sealed class GetPositionsQueryValidator : AbstractValidator<GetPositionsQuery>
{
    public GetPositionsQueryValidator() => RuleFor(x => x.Limit).InclusiveBetween(1, 100);
}
