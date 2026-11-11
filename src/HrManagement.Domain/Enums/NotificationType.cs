namespace HrManagement.Domain;

public enum NotificationType
{
    JourneyAssigned = 0,
    QuizUnlocked = 1,
    QuizFailed = 2,
    QuizPassed = 3,
    JourneyCompleted = 4,
    ReviewRequested = 5,
    StepOverdue = 6,
    LeaveRequested = 7,
    LeaveApproved = 8,
    LeaveRejected = 9,
    LeaveCancelled = 10
}
