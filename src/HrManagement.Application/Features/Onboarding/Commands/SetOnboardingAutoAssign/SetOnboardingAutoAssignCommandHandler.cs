using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.SetOnboardingAutoAssign;

public sealed class SetOnboardingAutoAssignCommandHandler(IOnboardingRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<SetOnboardingAutoAssignCommand, OnboardingTemplateDto>
{
    public async Task<OnboardingTemplateDto> Handle(SetOnboardingAutoAssignCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        if (template.Version != request.ExpectedVersion) throw new ConflictException("The template was changed by another user.");
        var now = DateTimeOffset.UtcNow;
        if (request.Enabled)
        {
            // Exactly one default package keeps "what does a new employee get?" unambiguous.
            foreach (var other in await repository.GetAutoAssignPackagesAsync(cancellationToken))
                if (other.Id != template.Id) other.SetAutoAssign(false, now);
        }
        template.SetAutoAssign(request.Enabled, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await repository.ListTemplatesAsync(false, cancellationToken)).Single(x => x.Id == template.Id);
    }
}
