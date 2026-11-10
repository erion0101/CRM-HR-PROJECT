using HrManagement.Domain;
using Xunit;

namespace HrManagement.Domain.Tests;

public sealed class OnboardingAssessmentTests
{
    [Fact]
    public void Failed_attempt_preserves_history_and_starts_fully_unread_cycle()
    {
        var organizationId = Guid.NewGuid();
        var assignment = OnboardingAssignment.Create(organizationId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            1, null, null, null, new DateOnly(2026, 10, 24));
        var templateStep = OnboardingTemplateStep.Create(organizationId, assignment.TemplateVersionId, "Company", 0,
            "Security policy", "Read the policy", "Understand it", OnboardingStepType.ReadDocument, 0, 0,
            true, null, Guid.NewGuid(), null, null);
        var step = OnboardingStepProgress.Create(organizationId, assignment.Id, templateStep, assignment.StartDate, null);
        var firstCycle = OnboardingReadingCycle.Create(organizationId, assignment.Id, 1, DateTimeOffset.UtcNow);
        var firstReading = OnboardingReadingProgress.Create(organizationId, firstCycle.Id, step, Guid.NewGuid());
        firstReading.Start(DateTimeOffset.UtcNow);
        firstReading.MarkRead(DateTimeOffset.UtcNow);
        assignment.ConfigureAssessment(firstCycle.Id, 1, true, DateTimeOffset.UtcNow);
        assignment.StartAssessment(DateTimeOffset.UtcNow);
        var attempt = OnboardingQuizAttempt.Create(organizationId, assignment.Id, firstCycle.Id, Guid.NewGuid(),
            assignment.EmployeeId, 1, 80, DateTimeOffset.UtcNow);

        var passed = attempt.Submit(6, 10, DateTimeOffset.UtcNow);
        firstCycle.Fail(attempt.Id, DateTimeOffset.UtcNow);
        step.ResetForReadingCycle(DateTimeOffset.UtcNow);
        var secondCycle = OnboardingReadingCycle.Create(organizationId, assignment.Id, 2, DateTimeOffset.UtcNow);
        var secondReading = OnboardingReadingProgress.Create(organizationId, secondCycle.Id, step, Guid.NewGuid());
        assignment.RestartAssessment(secondCycle.Id, 2, DateTimeOffset.UtcNow);

        Assert.False(passed);
        Assert.Equal(OnboardingQuizAttemptStatus.Failed, attempt.Status);
        Assert.Equal(60m, attempt.ScorePercentage);
        Assert.Equal(OnboardingReadingCycleStatus.Failed, firstCycle.Status);
        Assert.Equal(attempt.Id, firstCycle.FailedAttemptId);
        Assert.Equal(OnboardingReadingStatus.Read, firstReading.Status);
        Assert.Equal(OnboardingReadingStatus.Unread, secondReading.Status);
        Assert.Equal(OnboardingStepStatus.NotStarted, step.Status);
        Assert.Equal(2, assignment.CurrentReadingCycleNumber);
        Assert.Equal(OnboardingAssessmentStatus.Locked, assignment.AssessmentStatus);
    }

    [Fact]
    public void Passing_threshold_is_inclusive_and_completes_assessment()
    {
        var organizationId = Guid.NewGuid();
        var assignment = OnboardingAssignment.Create(organizationId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            1, null, null, null, new DateOnly(2026, 10, 24));
        var cycle = OnboardingReadingCycle.Create(organizationId, assignment.Id, 1, DateTimeOffset.UtcNow);
        assignment.ConfigureAssessment(cycle.Id, 1, true, DateTimeOffset.UtcNow);
        assignment.StartAssessment(DateTimeOffset.UtcNow);
        var attempt = OnboardingQuizAttempt.Create(organizationId, assignment.Id, cycle.Id, Guid.NewGuid(),
            assignment.EmployeeId, 1, 80, DateTimeOffset.UtcNow);

        Assert.True(attempt.Submit(8, 10, DateTimeOffset.UtcNow));
        cycle.Pass(DateTimeOffset.UtcNow);
        assignment.PassAssessment(DateTimeOffset.UtcNow);
        assignment.RefreshStatus(true, false, false, true, DateTimeOffset.UtcNow);

        Assert.Equal(OnboardingQuizAttemptStatus.Passed, attempt.Status);
        Assert.Equal(80m, attempt.ScorePercentage);
        Assert.Equal(OnboardingAssessmentStatus.Passed, assignment.AssessmentStatus);
        Assert.Equal(OnboardingAssignmentStatus.Completed, assignment.Status);
    }

    [Fact]
    public void Quiz_cannot_start_while_required_reading_is_locked()
    {
        var assignment = OnboardingAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            1, null, null, null, new DateOnly(2026, 10, 24));
        assignment.ConfigureAssessment(Guid.NewGuid(), 1, false, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainException>(() => assignment.StartAssessment(DateTimeOffset.UtcNow));

        Assert.Contains("locked", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Failed_cycle_recreates_every_reading_item_without_mutating_history()
    {
        var organizationId = Guid.NewGuid();
        var assignment = OnboardingAssignment.Create(organizationId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            1, null, null, null, new DateOnly(2026, 10, 24));
        var firstCycle = OnboardingReadingCycle.Create(organizationId, assignment.Id, 1, DateTimeOffset.UtcNow);
        var progresses = Enumerable.Range(0, 8).Select(index =>
        {
            var sourceDocumentId = Guid.NewGuid();
            var templateStep = OnboardingTemplateStep.Create(organizationId, assignment.TemplateVersionId,
                index < 4 ? "Company" : "Project", index < 4 ? 0 : 1, $"Document {index + 1}",
                "Read the document", "Understand it", OnboardingStepType.ReadDocument, index, 0,
                true, null, sourceDocumentId, null, null);
            return OnboardingStepProgress.Create(organizationId, assignment.Id, templateStep, assignment.StartDate, null);
        }).ToArray();
        var historical = progresses.Select(progress =>
        {
            var item = OnboardingReadingProgress.Create(organizationId, firstCycle.Id, progress, Guid.NewGuid());
            item.Start(DateTimeOffset.UtcNow);
            item.MarkRead(DateTimeOffset.UtcNow);
            return item;
        }).ToArray();

        var nextCycle = OnboardingReadingCycle.Create(organizationId, assignment.Id, 2, DateTimeOffset.UtcNow);
        var current = progresses.Select(progress =>
        {
            progress.ResetForReadingCycle(DateTimeOffset.UtcNow);
            return OnboardingReadingProgress.Create(organizationId, nextCycle.Id, progress, Guid.NewGuid());
        }).ToArray();

        Assert.All(historical, item => Assert.Equal(OnboardingReadingStatus.Read, item.Status));
        Assert.All(current, item => Assert.Equal(OnboardingReadingStatus.Unread, item.Status));
        Assert.All(progresses, progress => Assert.Equal(OnboardingStepStatus.NotStarted, progress.Status));
        Assert.Equal(8, current.Length);
    }

    [Fact]
    public void Document_cannot_be_marked_read_before_it_is_opened()
    {
        var organizationId = Guid.NewGuid();
        var templateStep = OnboardingTemplateStep.Create(organizationId, Guid.NewGuid(), "Company", 0,
            "Security policy", "Read the policy", "Understand it", OnboardingStepType.ReadDocument, 0, 0,
            true, null, Guid.NewGuid(), null, null);
        var step = OnboardingStepProgress.Create(organizationId, Guid.NewGuid(), templateStep, new DateOnly(2026, 10, 24), null);
        var reading = OnboardingReadingProgress.Create(organizationId, Guid.NewGuid(), step, Guid.NewGuid());

        var exception = Assert.Throws<DomainException>(() => reading.MarkRead(DateTimeOffset.UtcNow));
        Assert.Contains("Open the document", exception.Message, StringComparison.Ordinal);
        Assert.Equal(OnboardingReadingStatus.Unread, reading.Status);

        reading.Start(DateTimeOffset.UtcNow);
        reading.MarkRead(DateTimeOffset.UtcNow);
        Assert.Equal(OnboardingReadingStatus.Read, reading.Status);
    }

    [Fact]
    public void Submitted_attempt_cannot_be_submitted_again()
    {
        var attempt = OnboardingQuizAttempt.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), 1, 80, DateTimeOffset.UtcNow);
        attempt.Submit(7, 10, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainException>(() => attempt.Submit(10, 10, DateTimeOffset.UtcNow));

        Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(OnboardingQuizAttemptStatus.Failed, attempt.Status);
        Assert.Equal(70m, attempt.ScorePercentage);
    }
}
