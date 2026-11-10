using HrManagement.Domain;
using Xunit;

namespace HrManagement.Domain.Tests;

public sealed class NotificationTests
{
    [Theory]
    [InlineData("https://evil.example/phish")]
    [InlineData("//evil.example")]
    [InlineData("onboarding")]
    public void Links_must_stay_inside_the_application(string link) =>
        Assert.Throws<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.QuizUnlocked,
            "Title", "Message", link));

    [Fact]
    public void Marking_read_is_idempotent()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.QuizUnlocked, "Title", "Message", "/onboarding");
        var first = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        notification.MarkRead(first);
        notification.MarkRead(first.AddHours(1));

        Assert.Equal(first, notification.ReadAtUtc);
    }
}
