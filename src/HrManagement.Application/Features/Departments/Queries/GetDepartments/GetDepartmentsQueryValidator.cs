using FluentValidation;

namespace HrManagement.Application.Features.Departments.Queries.GetDepartments;

public sealed class GetDepartmentsQueryValidator : AbstractValidator<GetDepartmentsQuery>
{
    public GetDepartmentsQueryValidator() => RuleFor(x => x.Limit).InclusiveBetween(1, 100);
}
