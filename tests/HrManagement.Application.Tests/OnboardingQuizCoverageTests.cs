using HrManagement.Application.Features.Onboarding;
using HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingTemplate;
using HrManagement.Application.Features.Onboarding.Commands.RunOnboardingQuizGeneration;
using HrManagement.Domain;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class OnboardingQuizCoverageTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Random Seeded = new(42);

    [Fact]
    public void Attempt_draws_questions_from_every_document()
    {
        var documents = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        // Document 0 has far more questions than the others; it must not dominate the attempt.
        var pool = documents.SelectMany((document, index) => Questions(document, index == 0 ? 20 : 3)).ToArray();

        var selected = QuizQuestionSelector.Select(pool, 10, new HashSet<Guid>(), true, Seeded.Next);

        Assert.Equal(10, selected.Count);
        Assert.Equal(10, selected.Select(x => x.Id).Distinct().Count());
        Assert.All(documents, document => Assert.Contains(selected, question => question.SourceDocumentId == document));
    }

    [Fact]
    public void Retake_prefers_questions_not_shown_in_previous_attempt()
    {
        var pool = Enumerable.Range(0, 3).SelectMany(_ => Questions(Guid.NewGuid(), 4)).ToArray();
        var first = QuizQuestionSelector.Select(pool, 6, new HashSet<Guid>(), true, Seeded.Next);

        var second = QuizQuestionSelector.Select(pool, 6, first.Select(x => x.Id).ToHashSet(), true, Seeded.Next);

        Assert.Empty(second.Select(x => x.Id).Intersect(first.Select(x => x.Id)));
    }

    [Fact]
    public void Without_randomization_selection_is_deterministic()
    {
        var pool = Questions(Guid.NewGuid(), 5);

        var selected = QuizQuestionSelector.Select(pool, 3, new HashSet<Guid>(), false, Seeded.Next);

        Assert.Equal(pool.Take(3).Select(x => x.Id), selected.Select(x => x.Id));
    }

    [Fact]
    public void Evidence_includes_every_document_even_when_the_first_is_huge()
    {
        var excerpts = new List<QuizSourceExcerpt>();
        excerpts.AddRange(Excerpts("A handbook", 40, 4_000));
        excerpts.AddRange(Excerpts("B security policy", 3, 1_000));
        excerpts.AddRange(Excerpts("Z project architecture", 3, 1_000));

        var result = QuizEvidenceBuilder.Build(excerpts, 30_000);

        Assert.Equal(3, result.DocumentLabels.Count);
        Assert.Contains(result.Included, x => x.Label == "Z project architecture");
        Assert.Contains(result.Included, x => x.Label == "B security policy");
        Assert.True(result.Evidence.Length < 32_000);
        // The large document is sampled from start to end, not only its first pages.
        var handbookPages = result.Included.Where(x => x.Label == "A handbook").Select(x => x.PageNumber!.Value).ToArray();
        Assert.Contains(handbookPages, page => page > 20);
    }

    [Theory]
    [InlineData(10, 9, 20)]
    [InlineData(5, 9, 14)]
    [InlineData(30, 5, 40)]
    [InlineData(50, 5, 50)]
    public void Generated_bank_is_larger_than_one_attempt(int perAttempt, int documents, int expected) =>
        Assert.Equal(expected, RunOnboardingQuizGenerationCommandHandler.TargetBankSize(perAttempt, documents));

    [Fact]
    public void Combined_assessment_uses_the_stricter_pass_mark_and_both_question_counts()
    {
        var company = OnboardingQuiz.Create(OrganizationId, Guid.NewGuid(), 1, "Company", 80, 10, true, true,
            OnboardingQuizGenerationSource.Manual);
        var project = OnboardingQuiz.Create(OrganizationId, Guid.NewGuid(), 1, "Project", 90, 5, true, true,
            OnboardingQuizGenerationSource.Manual);

        Assert.Equal(90, CombinedAssessment.PassingPercentage([company, project]));
        Assert.Equal(15, CombinedAssessment.QuestionCount([company, project]));
        Assert.Equal(80, CombinedAssessment.PassingPercentage([company]));
    }

    [Fact]
    public void Project_package_requires_a_project_and_other_kinds_reject_one()
    {
        var validator = new CreateOnboardingTemplateCommandValidator();
        OnboardingTemplateStepInput[] steps = [new("Week 1", 0, "Read", "Read it", "Know it", OnboardingStepType.PracticalTask, 0, 1, true, null, null, null, null)];

        Assert.False(validator.Validate(new CreateOnboardingTemplateCommand("Portal", "Docs", null, null, 30, steps, OnboardingTemplateKind.Project)).IsValid);
        Assert.False(validator.Validate(new CreateOnboardingTemplateCommand("Company", "Docs", null, null, 30, steps, OnboardingTemplateKind.Company, Guid.NewGuid())).IsValid);
        Assert.True(validator.Validate(new CreateOnboardingTemplateCommand("Portal", "Docs", null, null, 30, steps, OnboardingTemplateKind.Project, Guid.NewGuid())).IsValid);
    }

    private static OnboardingQuizQuestion[] Questions(Guid documentId, int count) =>
        Enumerable.Range(0, count).Select(index => OnboardingQuizQuestion.Create(OrganizationId, Guid.NewGuid(),
            $"Question {index}?", "Explanation", "Topic", OnboardingQuizDifficulty.Medium, index,
            documentId, Guid.NewGuid(), null, $"Document {documentId}", null, null)).ToArray();

    private static IEnumerable<QuizSourceExcerpt> Excerpts(string label, int chunks, int length)
    {
        var documentId = Guid.NewGuid();
        return Enumerable.Range(0, chunks).Select(index => new QuizSourceExcerpt($"source:{Guid.NewGuid()}", documentId,
            Guid.NewGuid(), label, new string('x', length), index + 1, null));
    }
}
