namespace HrManagement.Application.Features.Onboarding;

/// <summary>Worker job: notifies employees and their reviewer once when a required step becomes overdue.</summary>
public interface IOnboardingOverdueReminderProcessor
{
    Task<int> ProcessAsync(CancellationToken cancellationToken);
}
