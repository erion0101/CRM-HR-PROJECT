using HrManagement.Application.Features.Onboarding;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Runs across organizations in the worker. Each required, unfinished step whose due date has passed gets one reminder
/// for the employee and one for the journey's reviewer; the step is then flagged so it is never reminded twice.
/// The scan runs at most every <see cref="Interval"/> because due dates only change daily.
/// </summary>
internal sealed class OnboardingOverdueReminderProcessor(AppDbContext db) : IOnboardingOverdueReminderProcessor
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private const int BatchSize = 200;
    private static DateTimeOffset _nextRunUtc = DateTimeOffset.MinValue;

    public async Task<int> ProcessAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now < _nextRunUtc) return 0;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var overdue = await (from step in db.OnboardingStepProgresses
                             join assignment in db.OnboardingAssignments on step.AssignmentId equals assignment.Id
                             join employee in db.Employees on assignment.EmployeeId equals employee.Id
                             where step.IsRequired && step.Status != OnboardingStepStatus.Completed && step.DueDate < today &&
                                   step.OverdueNotifiedAtUtc == null &&
                                   assignment.Status != OnboardingAssignmentStatus.Completed &&
                                   assignment.Status != OnboardingAssignmentStatus.Cancelled &&
                                   assignment.Status != OnboardingAssignmentStatus.Superseded
                             orderby step.DueDate
                             select new { Step = step, assignment.OrganizationId, assignment.EmployeeId, assignment.ManagerEmployeeId,
                                 EmployeeName = employee.FirstName + " " + employee.LastName })
            .Take(BatchSize).ToListAsync(cancellationToken);

        foreach (var item in overdue)
        {
            var due = item.Step.DueDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            db.Notifications.Add(Notification.Create(item.OrganizationId, item.EmployeeId, NotificationType.StepOverdue,
                "An onboarding step is overdue", $"\"{item.Step.Title}\" was due on {due}.", "/onboarding"));
            if (item.ManagerEmployeeId is { } manager && manager != item.EmployeeId)
                db.Notifications.Add(Notification.Create(item.OrganizationId, manager, NotificationType.StepOverdue,
                    $"{item.EmployeeName} has an overdue onboarding step", $"\"{item.Step.Title}\" was due on {due}.", "/onboarding/insights"));
            item.Step.MarkOverdueNotified(now);
        }
        if (overdue.Count > 0) await db.SaveChangesAsync(cancellationToken);
        // A full batch means there may be more; check again on the next worker tick.
        if (overdue.Count < BatchSize) _nextRunUtc = now.Add(Interval);
        return overdue.Count;
    }
}
