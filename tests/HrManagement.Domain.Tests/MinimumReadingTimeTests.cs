using HrManagement.Domain;
using Xunit;

namespace HrManagement.Domain.Tests;

public sealed class MinimumReadingTimeTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly DateTimeOffset Opened = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private static OnboardingReadingProgress Reading(int? minimumMinutes, OnboardingStepType type = OnboardingStepType.ReadDocument)
    {
        var templateStep = OnboardingTemplateStep.Create(OrganizationId, Guid.NewGuid(), "Company", 0, "Security policy",
            "Read it", "Understand it", type, 0, 3, true, null, Guid.NewGuid(), null, null, minimumMinutes);
        var step = OnboardingStepProgress.Create(OrganizationId, Guid.NewGuid(), templateStep, new DateOnly(2026, 11, 10), null);
        return OnboardingReadingProgress.Create(OrganizationId, Guid.NewGuid(), step, Guid.NewGuid());
    }

    [Fact]
    public void Cannot_mark_read_before_the_minimum_time_since_first_opening()
    {
        var reading = Reading(3);
        reading.Start(Opened);

        var tooSoon = Assert.Throws<DomainException>(() => reading.MarkRead(Opened.AddSeconds(70)));
        Assert.Contains("about 2 minutes", tooSoon.Message, StringComparison.Ordinal);
        Assert.Equal(OnboardingReadingStatus.InProgress, reading.Status);

        reading.MarkRead(Opened.AddMinutes(3));
        Assert.Equal(OnboardingReadingStatus.Read, reading.Status);
    }

    [Fact]
    public void Reopening_does_not_restart_the_reading_clock()
    {
        var reading = Reading(2);
        reading.Start(Opened);
        reading.Start(Opened.AddMinutes(1));

        Assert.Equal(Opened.AddMinutes(2), reading.ReadableAtUtc);
        reading.MarkRead(Opened.AddMinutes(2));
        Assert.Equal(OnboardingReadingStatus.Read, reading.Status);
    }

    [Fact]
    public void Without_a_minimum_an_opened_document_can_be_marked_read_immediately()
    {
        var reading = Reading(null);
        reading.Start(Opened);

        Assert.Null(reading.ReadableAtUtc);
        reading.MarkRead(Opened);
        Assert.Equal(OnboardingReadingStatus.Read, reading.Status);
    }

    [Fact]
    public void Minimum_must_be_in_range_and_applies_only_to_reading_steps()
    {
        Assert.Throws<DomainException>(() => Reading(0));
        Assert.Throws<DomainException>(() => Reading(OnboardingTemplateStep.MaxMinimumReadingMinutes + 1));

        var practical = OnboardingTemplateStep.Create(OrganizationId, Guid.NewGuid(), "Week 1", 0, "Meet the team",
            "Say hello", "Know the team", OnboardingStepType.PracticalTask, 0, 1, true, null, null, null, null, 5);
        Assert.Null(practical.MinimumReadingMinutes);
    }
}
