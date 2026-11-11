using HrManagement.Application.Features.Notifications;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave;

/// <summary>In-app notifications for time off, saved in the same unit of work as the change that caused them.</summary>
public sealed class LeaveNotifier(INotificationRepository notifications, ILeaveRepository repository)
{
    private const string EmployeeLink = "/time-off";
    private const string ApproverLink = "/time-off/approvals";

    public async Task RequestedAsync(LeaveRequest request, LeaveType type, CancellationToken cancellationToken)
    {
        if (request.Status != LeaveRequestStatus.Pending || request.ApproverEmployeeId is not { } approver) return;
        var name = await NameAsync(request.EmployeeId, cancellationToken);
        Notify(request, approver, NotificationType.LeaveRequested, $"{name} asked for time off",
            $"{type.Name}, {Period(request)} ({Days(request)}).", ApproverLink);
    }

    public void Approved(LeaveRequest request, LeaveType type) =>
        Notify(request, request.EmployeeId, NotificationType.LeaveApproved, "Your time off is approved",
            $"{type.Name}, {Period(request)} ({Days(request)}).", EmployeeLink);

    public void Rejected(LeaveRequest request, LeaveType type) =>
        Notify(request, request.EmployeeId, NotificationType.LeaveRejected, "Your time off was not approved",
            $"{type.Name}, {Period(request)}. Reason: {request.DecisionComment}", EmployeeLink);

    /// <summary>Tells the other side: the approver when the employee cancels, the employee when HR cancels for them.</summary>
    public async Task CancelledAsync(LeaveRequest request, LeaveType type, Guid? cancelledByEmployeeId, CancellationToken cancellationToken)
    {
        if (cancelledByEmployeeId == request.EmployeeId)
        {
            if (request.ApproverEmployeeId is not { } approver) return;
            var name = await NameAsync(request.EmployeeId, cancellationToken);
            Notify(request, approver, NotificationType.LeaveCancelled, $"{name} cancelled time off",
                $"{type.Name}, {Period(request)}.", ApproverLink);
        }
        else
        {
            Notify(request, request.EmployeeId, NotificationType.LeaveCancelled, "Your time off was cancelled by HR",
                $"{type.Name}, {Period(request)}.", EmployeeLink);
        }
    }

    private static string Period(LeaveRequest x) => x.StartDate == x.EndDate
        ? x.StartDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)
        : $"{x.StartDate.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture)} – {x.EndDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)}";

    private static string Days(LeaveRequest x) => x.WorkingDays == 1 ? "1 working day" : $"{x.WorkingDays} working days";

    private async Task<string> NameAsync(Guid employeeId, CancellationToken cancellationToken) =>
        (await repository.GetEmployeeNamesAsync([employeeId], cancellationToken)).GetValueOrDefault(employeeId, "An employee");

    private void Notify(LeaveRequest request, Guid recipient, NotificationType type, string title, string message, string link) =>
        notifications.Add(Notification.Create(request.OrganizationId, recipient, type, title, message, link));
}
