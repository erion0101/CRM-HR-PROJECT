using FluentValidation;

namespace HrManagement.Application.Features.AuditEvents.Queries.GetEmployeeActivity;

public sealed class GetEmployeeActivityQueryValidator : AbstractValidator<GetEmployeeActivityQuery>
{
    public GetEmployeeActivityQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
