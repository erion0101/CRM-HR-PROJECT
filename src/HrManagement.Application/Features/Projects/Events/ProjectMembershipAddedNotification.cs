using MediatR;

namespace HrManagement.Application.Features.Projects.Events;

/// <summary>Raised after an employee is added to a project by someone allowed to manage that project.</summary>
public sealed record ProjectMembershipAddedNotification(Guid OrganizationId, Guid ProjectId, Guid EmployeeId, DateOnly EffectiveFrom) : INotification;
