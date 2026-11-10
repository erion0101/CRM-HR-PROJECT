using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.GenerateOnboardingQuiz;

/// <summary>
/// Queues AI question generation for a draft quiz. The background worker does the slow provider call,
/// so this request returns immediately and the UI polls the quiz until generation completes or fails.
/// Cheap preconditions are checked here so HR gets immediate feedback.
/// </summary>
public sealed class GenerateOnboardingQuizCommandHandler(
    IOnboardingRepository repository,
    IAiRuntimeSettings settings,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<GenerateOnboardingQuizCommand, OnboardingQuizAdminDto>
{
    public async Task<OnboardingQuizAdminDto> Handle(GenerateOnboardingQuizCommand request, CancellationToken cancellationToken)
    {
        var quiz = await repository.GetQuizAsync(request.QuizId, cancellationToken) ?? throw new NotFoundException("Quiz not found.");
        if (quiz.Status != OnboardingQuizStatus.Draft) throw new ConflictException("Published quizzes are immutable.");
        if (quiz.IsGenerating) throw new ConflictException("AI generation is already in progress for this quiz.");
        if ((await repository.GetQuizQuestionsAsync(quiz.Id, cancellationToken)).Count != 0)
            throw new ConflictException("AI generation requires an empty draft. Remove existing questions first.");
        if (!settings.Value.Enabled || !settings.Value.RealDataApproved || !settings.ProviderConfigured)
            throw new ValidationException(new Dictionary<string, string[]> { ["ai"] = ["AI quiz generation is disabled or governance approval/provider configuration is incomplete. Create questions manually instead."] });
        if ((await repository.GetQuizSourceExcerptsAsync(quiz.TemplateVersionId, cancellationToken)).Count == 0)
            throw new ValidationException(new Dictionary<string, string[]> { ["documents"] = ["No approved, processed PDF/DOCX text is available. Approve and index the required documents, or create questions manually."] });

        quiz.QueueGeneration(currentUser.UserId ?? throw new ForbiddenException(), DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetQuizAdminDtoAsync(quiz.Id, cancellationToken) ?? throw new InvalidOperationException("Queued quiz was not available.");
    }
}
