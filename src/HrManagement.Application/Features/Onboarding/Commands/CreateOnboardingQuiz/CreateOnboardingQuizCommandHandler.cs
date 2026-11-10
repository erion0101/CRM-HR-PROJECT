using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingQuiz;

public sealed class CreateOnboardingQuizCommandHandler(IOnboardingRepository repository, IAuthorizedDataScope scope, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOnboardingQuizCommand, OnboardingQuizAdminDto>
{
    public async Task<OnboardingQuizAdminDto> Handle(CreateOnboardingQuizCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        if (template.PublishedVersion is not { } versionNumber) throw new ConflictException("Publish the onboarding template before creating its assessment.");
        var version = await repository.GetTemplateVersionAsync(template.Id, versionNumber, cancellationToken) ?? throw new NotFoundException("Published template version not found.");
        if (await repository.GetDraftQuizAsync(version.Id, cancellationToken) is not null) throw new ConflictException("This onboarding version already has a draft quiz.");
        if (await repository.GetPublishedQuizAsync(version.Id, cancellationToken) is not null) throw new ConflictException("The published quiz is immutable. Create a new onboarding template version to change it.");
        var quiz = OnboardingQuiz.Create(scope.OrganizationId, version.Id, await repository.GetNextQuizVersionAsync(version.Id, cancellationToken),
            request.Title, request.PassingPercentage, request.QuestionCount, request.RandomizeQuestions, request.RandomizeOptions,
            OnboardingQuizGenerationSource.Manual);
        repository.Add(quiz);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetQuizAdminDtoAsync(quiz.Id, cancellationToken) ?? throw new InvalidOperationException("Created quiz was not available.");
    }
}
