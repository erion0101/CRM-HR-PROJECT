using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Onboarding.Insights;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class OnboardingInsightsReader(AppDbContext db, ICurrentUserService currentUser, IAuthorizedDataScope scope)
    : IOnboardingInsightsReader
{
    public async Task<OnboardingInsightsData> ReadAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        // A manager's own journey is visible to them elsewhere, but metrics are about the people they look after.
        var employeeIds = currentUser.EmployeeId is { } self && !currentUser.IsInRole(Application.Common.Authorization.Roles.Hr)
            ? allowed.Where(x => x != self).ToArray()
            : allowed.ToArray();

        var assignments = db.OnboardingAssignments.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
            employeeIds.Contains(x.EmployeeId) &&
            x.Status != OnboardingAssignmentStatus.Cancelled && x.Status != OnboardingAssignmentStatus.Superseded);

        var journeys = await (from assignment in assignments
                              join employee in db.Employees.AsNoTracking() on assignment.EmployeeId equals employee.Id
                              join template in db.OnboardingTemplates.AsNoTracking() on assignment.TemplateId equals template.Id
                              select new
                              {
                                  assignment.Id,
                                  assignment.EmployeeId,
                                  EmployeeName = employee.FirstName + " " + employee.LastName,
                                  template.Name,
                                  assignment.Status,
                                  assignment.StartDate,
                                  assignment.CompletedAtUtc
                              }).ToListAsync(cancellationToken);
        var assignmentIds = journeys.Select(x => x.Id).ToArray();

        var stepCounts = await db.OnboardingStepProgresses.AsNoTracking()
            .Where(x => assignmentIds.Contains(x.AssignmentId))
            .GroupBy(x => x.AssignmentId)
            .Select(g => new
            {
                AssignmentId = g.Key,
                Overdue = g.Count(x => x.IsRequired && x.Status != OnboardingStepStatus.Completed && x.DueDate < today),
                PendingReview = g.Count(x => x.Status == OnboardingStepStatus.PendingReview)
            }).ToDictionaryAsync(x => x.AssignmentId, cancellationToken);

        var attempts = await db.OnboardingQuizAttempts.AsNoTracking()
            .Where(x => assignmentIds.Contains(x.AssignmentId))
            .Select(x => new InsightAttemptRow(x.AssignmentId, x.AttemptNumber, x.Status))
            .ToListAsync(cancellationToken);

        var answers = await (from answer in db.OnboardingQuizAnswers.AsNoTracking()
                             join attempt in db.OnboardingQuizAttempts.AsNoTracking() on answer.AttemptId equals attempt.Id
                             join question in db.OnboardingQuizQuestions.AsNoTracking() on answer.QuestionId equals question.Id
                             where assignmentIds.Contains(attempt.AssignmentId)
                             group answer by new { question.SourceDocumentId, question.SourceLabel } into g
                             select new InsightAnswerRow(g.Key.SourceDocumentId, g.Key.SourceLabel, g.Count(), g.Count(x => !x.IsCorrect)))
            .ToListAsync(cancellationToken);

        return new OnboardingInsightsData(
            journeys.Select(x => new InsightJourneyRow(x.Id, x.EmployeeId, x.EmployeeName, x.Name, x.Status, x.StartDate, x.CompletedAtUtc,
                stepCounts.GetValueOrDefault(x.Id)?.Overdue ?? 0, stepCounts.GetValueOrDefault(x.Id)?.PendingReview ?? 0)).ToArray(),
            attempts,
            answers);
    }
}
