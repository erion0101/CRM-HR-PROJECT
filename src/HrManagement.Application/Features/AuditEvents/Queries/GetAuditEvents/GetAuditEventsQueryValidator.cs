using FluentValidation;

namespace HrManagement.Application.Features.AuditEvents.Queries.GetAuditEvents;

public sealed class GetAuditEventsQueryValidator : AbstractValidator<GetAuditEventsQuery>
{
    public GetAuditEventsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.EntityType).MaximumLength(150);
    }
}
