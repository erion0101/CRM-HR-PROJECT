using HrManagement.Application.Common.Exceptions;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

internal static class OnboardingReadingCycleFactory
{
    public static async Task<OnboardingReadingCycle> CreateAsync(IOnboardingRepository repository,
        OnboardingAssignment assignment, IReadOnlyList<OnboardingStepProgress> steps, int cycleNumber,
        DateTimeOffset now, CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, Guid?>? documentVersionSnapshot = null)
    {
        var cycle = OnboardingReadingCycle.Create(assignment.OrganizationId, assignment.Id, cycleNumber, now);
        repository.Add(cycle);
        foreach (var step in steps.Where(IsReadingStep))
        {
            Guid? documentVersionId = null;
            if (step.DocumentId is { } documentId)
            {
                if (documentVersionSnapshot is not null && documentVersionSnapshot.TryGetValue(step.Id, out var snapshottedVersionId))
                    documentVersionId = snapshottedVersionId;
                else
                    documentVersionId = await repository.GetCurrentDocumentVersionIdAsync(documentId, cancellationToken);
                if (documentVersionId is null)
                    throw new ConflictException($"Required document '{step.Title}' is not clean and available.");
            }
            repository.Add(OnboardingReadingProgress.Create(assignment.OrganizationId, cycle.Id, step, documentVersionId));
        }
        return cycle;
    }

    public static bool IsReadingStep(OnboardingStepProgress step) =>
        step.StepType is OnboardingStepType.ReadDocument or OnboardingStepType.ReadArticle or OnboardingStepType.PolicyAcknowledgment;
}
