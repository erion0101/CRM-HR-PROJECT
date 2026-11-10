using HrManagement.Domain;
using Xunit;

namespace HrManagement.Domain.Tests;

public sealed class OnboardingAutomationTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private static OnboardingQuiz DraftQuiz() =>
        OnboardingQuiz.Create(OrganizationId, Guid.NewGuid(), 1, "Company quiz", 80, 10, true, true, OnboardingQuizGenerationSource.Manual);

    [Fact]
    public void Generation_moves_from_queued_to_running_to_completed()
    {
        var quiz = DraftQuiz();
        var requester = Guid.NewGuid();

        quiz.QueueGeneration(requester, Now);
        Assert.Equal(OnboardingQuizGenerationStatus.Queued, quiz.GenerationStatus);
        Assert.Equal(requester, quiz.GenerationRequestedByUserId);
        Assert.Throws<DomainException>(() => quiz.QueueGeneration(requester, Now));

        Assert.True(quiz.TryLeaseGeneration(Now, TimeSpan.FromMinutes(4)));
        Assert.Equal(OnboardingQuizGenerationStatus.Running, quiz.GenerationStatus);
        Assert.False(quiz.TryLeaseGeneration(Now.AddMinutes(1), TimeSpan.FromMinutes(4)));

        quiz.CompleteGeneration(Now);
        Assert.Equal(OnboardingQuizGenerationStatus.Completed, quiz.GenerationStatus);
        Assert.Equal(OnboardingQuizGenerationSource.AiDraft, quiz.GenerationSource);
    }

    [Fact]
    public void Stalled_generation_is_retried_then_failed()
    {
        var quiz = DraftQuiz();
        quiz.QueueGeneration(Guid.NewGuid(), Now);
        var at = Now;
        for (var attempt = 1; attempt <= OnboardingQuiz.MaxGenerationAttempts; attempt++)
        {
            Assert.True(quiz.TryLeaseGeneration(at, TimeSpan.FromMinutes(1)));
            at = at.AddMinutes(2); // lease expires: the worker crashed or timed out
        }

        Assert.False(quiz.TryLeaseGeneration(at, TimeSpan.FromMinutes(1)));
        Assert.Equal(OnboardingQuizGenerationStatus.Failed, quiz.GenerationStatus);
        Assert.NotNull(quiz.GenerationError);
    }

    [Fact]
    public void Failed_generation_can_be_queued_again()
    {
        var quiz = DraftQuiz();
        quiz.QueueGeneration(Guid.NewGuid(), Now);
        quiz.TryLeaseGeneration(Now, TimeSpan.FromMinutes(1));
        quiz.FailGeneration("Provider timeout", Now);

        quiz.QueueGeneration(Guid.NewGuid(), Now);

        Assert.Equal(OnboardingQuizGenerationStatus.Queued, quiz.GenerationStatus);
        Assert.Null(quiz.GenerationError);
        Assert.Equal(0, quiz.GenerationAttempts);
    }

    [Fact]
    public void Only_a_published_company_package_can_be_assigned_automatically()
    {
        var project = OnboardingTemplate.Create(OrganizationId, "Portal", "Project docs", null, null, 30,
            OnboardingTemplateKind.Project, Guid.NewGuid());
        project.Publish(Now);
        Assert.Throws<DomainException>(() => project.SetAutoAssign(true, Now));

        var company = OnboardingTemplate.Create(OrganizationId, "Company", "Company docs", null, null, 30, OnboardingTemplateKind.Company);
        Assert.Throws<DomainException>(() => company.SetAutoAssign(true, Now));

        company.Publish(Now);
        company.SetAutoAssign(true, Now);
        Assert.True(company.AutoAssignToNewEmployees);

        company.Retire(Now);
        Assert.False(company.AutoAssignToNewEmployees);
    }
}
