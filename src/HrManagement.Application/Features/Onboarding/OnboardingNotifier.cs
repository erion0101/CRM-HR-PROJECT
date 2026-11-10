using HrManagement.Application.Features.Notifications;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

/// <summary>
/// Creates the in-app notifications for onboarding events. Notifications are added to the caller's unit of work,
/// so they are saved together with the change that caused them (or not at all).
/// The journey's manager is its reviewer; mentors are only used as a fallback reviewer for milestones.
/// </summary>
internal sealed class OnboardingNotifier(INotificationRepository notifications, IOnboardingRepository repository)
{
    private const string EmployeeLink = "/onboarding";
    private const string ManagerLink = "/onboarding/manage";

    public void JourneyAssigned(OnboardingAssignment assignment, string journeyName) =>
        Notify(assignment, assignment.EmployeeId, NotificationType.JourneyAssigned, "New onboarding journey",
            $"\"{journeyName}\" has been assigned to you. Start with the first step whenever you are ready.", EmployeeLink);

    public void QuizUnlocked(OnboardingAssignment assignment) =>
        Notify(assignment, assignment.EmployeeId, NotificationType.QuizUnlocked, "Your onboarding quiz is unlocked",
            "You have read every required document. You can take the quiz now.", EmployeeLink);

    public async Task QuizSubmittedAsync(OnboardingAssignment assignment, OnboardingQuizAttempt attempt, bool passed, CancellationToken cancellationToken)
    {
        if (Reviewer(assignment) is not { } reviewer) return;
        var name = await EmployeeNameAsync(assignment, cancellationToken);
        var score = $"{attempt.ScorePercentage ?? 0:0.#}%";
        if (passed)
            Notify(assignment, reviewer, NotificationType.QuizPassed, $"{name} passed the onboarding quiz",
                $"Attempt {attempt.AttemptNumber} scored {score} (pass mark {attempt.PassingPercentage}%).", ManagerLink);
        else
            Notify(assignment, reviewer, NotificationType.QuizFailed, $"{name} did not pass the onboarding quiz",
                $"Attempt {attempt.AttemptNumber} scored {score} (pass mark {attempt.PassingPercentage}%). Their reading has been reset.", ManagerLink);
    }

    public async Task ReviewRequestedAsync(OnboardingAssignment assignment, OnboardingStepProgress step, CancellationToken cancellationToken)
    {
        var reviewer = Reviewer(assignment) ?? (assignment.MentorEmployeeId != assignment.EmployeeId ? assignment.MentorEmployeeId : null);
        if (reviewer is null) return;
        var name = await EmployeeNameAsync(assignment, cancellationToken);
        Notify(assignment, reviewer.Value, NotificationType.ReviewRequested, $"{name} is waiting for your review",
            $"\"{step.Title}\" was submitted for review.", ManagerLink);
    }

    public async Task JourneyCompletedAsync(OnboardingAssignment assignment, CancellationToken cancellationToken)
    {
        Notify(assignment, assignment.EmployeeId, NotificationType.JourneyCompleted, "Onboarding complete",
            "You have completed every required step of your onboarding journey. Welcome aboard!", EmployeeLink);
        if (Reviewer(assignment) is { } reviewer)
        {
            var name = await EmployeeNameAsync(assignment, cancellationToken);
            Notify(assignment, reviewer, NotificationType.JourneyCompleted, $"{name} completed onboarding",
                "Every required step, including the quiz, is complete.", ManagerLink);
        }
    }

    private static Guid? Reviewer(OnboardingAssignment assignment) =>
        assignment.ManagerEmployeeId is { } manager && manager != assignment.EmployeeId ? manager : null;

    private async Task<string> EmployeeNameAsync(OnboardingAssignment assignment, CancellationToken cancellationToken) =>
        await repository.GetEmployeeNameAsync(assignment.EmployeeId, cancellationToken) ?? "An employee";

    private void Notify(OnboardingAssignment assignment, Guid recipient, NotificationType type, string title, string message, string link) =>
        notifications.Add(Notification.Create(assignment.OrganizationId, recipient, type, title, message, link));
}
