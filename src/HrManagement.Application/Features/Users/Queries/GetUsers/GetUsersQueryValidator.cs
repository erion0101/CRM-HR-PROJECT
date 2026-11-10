using FluentValidation;

namespace HrManagement.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator() => RuleFor(x => x.Limit).InclusiveBetween(1, 100);
}
