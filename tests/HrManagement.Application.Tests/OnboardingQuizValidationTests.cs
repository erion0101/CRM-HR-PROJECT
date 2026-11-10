using HrManagement.Application.Features.Onboarding;
using HrManagement.Domain;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class OnboardingQuizValidationTests
{
    [Fact]
    public void Question_requires_four_options_and_exactly_one_correct_answer()
    {
        var sourceId = Guid.NewGuid();
        var invalid = new QuizQuestionInput("Question?", "Explanation", "Topic", OnboardingQuizDifficulty.Medium,
            0, Guid.NewGuid(), sourceId, null, "Source", null, null,
            [new("A", true, 0), new("B", true, 1), new("C", false, 2)]);

        var result = new QuizQuestionInputValidator().Validate(invalid);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(QuizQuestionInput.Options));
    }

    [Fact]
    public void Employee_quiz_option_contract_never_contains_answer_key()
    {
        var propertyNames = typeof(OnboardingQuizOptionDto).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("IsCorrect", propertyNames);
    }
}
