using MediatR;

namespace HrManagement.Application.Features.Employees.Events;

/// <summary>
/// Raised after an employee's employment first becomes Active (a new hire, or a draft employment being activated).
/// Other features react to it without the Employees feature depending on them.
/// </summary>
public sealed record EmploymentActivatedNotification(Guid EmployeeId, DateOnly StartDate) : INotification;
