using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Leave.Commands.AdjustLeaveBalance;
using HrManagement.Application.Features.Leave.Commands.CancelLeaveRequest;
using HrManagement.Application.Features.Leave.Commands.DecideLeaveRequest;
using HrManagement.Application.Features.Leave.Commands.PublicHolidays;
using HrManagement.Application.Features.Leave.Commands.RequestLeave;
using HrManagement.Application.Features.Leave.Commands.SaveLeaveType;
using HrManagement.Application.Features.Leave.Queries.EmployeeLeave;
using HrManagement.Application.Features.Leave.Queries.LeaveApprovals;
using HrManagement.Application.Features.Leave.Queries.LeaveCalendar;
using HrManagement.Application.Features.Leave.Queries.LeaveSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

/// <summary>Time off: settings (HR), the signed-in employee's own leave, approvals (managers and HR), and the team calendar.</summary>
[Route("api/v1/leave"), Authorize]
public sealed class LeaveController(ISender sender) : ApiControllerBase
{
    public sealed record DecisionBody(string? Comment);
    public sealed record AdjustmentBody(Guid LeaveTypeId, int Year, decimal Days, string Reason);

    [HttpGet("types")]
    public async Task<IActionResult> Types([FromQuery] bool includeArchived = false, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetLeaveTypesQuery(includeArchived), cancellationToken));

    [HttpPost("types"), Authorize(Policy = Policies.LeaveAdmin)]
    public async Task<IActionResult> CreateType(LeaveTypeInput body, CancellationToken cancellationToken)
    {
        var type = await sender.Send(new CreateLeaveTypeCommand(body), cancellationToken);
        return CreatedWithEtag($"/api/v1/leave/types/{type.Id}", type, type.Version);
    }

    [HttpPut("types/{id:guid}"), Authorize(Policy = Policies.LeaveAdmin)]
    public async Task<IActionResult> UpdateType(Guid id, LeaveTypeInput body, CancellationToken cancellationToken)
    {
        var type = await sender.Send(new UpdateLeaveTypeCommand(id, body, ExpectedVersion()), cancellationToken);
        return WithEtag(type, type.Version);
    }

    [HttpDelete("types/{id:guid}"), Authorize(Policy = Policies.LeaveAdmin)]
    public async Task<IActionResult> ArchiveType(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveLeaveTypeCommand(id, ExpectedVersion()), cancellationToken);
        return NoContent();
    }

    [HttpGet("holidays")]
    public async Task<IActionResult> Holidays(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPublicHolidaysQuery(), cancellationToken));

    [HttpPost("holidays"), Authorize(Policy = Policies.LeaveAdmin)]
    public async Task<IActionResult> CreateHoliday(CreatePublicHolidayCommand body, CancellationToken cancellationToken) =>
        Ok(await sender.Send(body, cancellationToken));

    [HttpDelete("holidays/{id:guid}"), Authorize(Policy = Policies.LeaveAdmin)]
    public async Task<IActionResult> DeleteHoliday(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePublicHolidayCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Mine([FromQuery] int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyLeaveQuery(year), cancellationToken));

    [HttpGet("me/preview")]
    public async Task<IActionResult> Preview([FromQuery] Guid leaveTypeId, [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PreviewLeaveRequestQuery(leaveTypeId, startDate, endDate), cancellationToken));

    [HttpPost("requests")]
    public async Task<IActionResult> RequestTimeOff(RequestLeaveCommand body, CancellationToken cancellationToken) =>
        Ok(await sender.Send(body, cancellationToken));

    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelLeaveRequestCommand(id), cancellationToken));

    [HttpGet("approvals"), Authorize(Policy = Policies.LeaveApprove)]
    public async Task<IActionResult> Approvals(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLeaveApprovalsQuery(), cancellationToken));

    [HttpPost("requests/{id:guid}/approve"), Authorize(Policy = Policies.LeaveApprove)]
    public async Task<IActionResult> Approve(Guid id, DecisionBody body, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DecideLeaveRequestCommand(id, true, body.Comment), cancellationToken));

    [HttpPost("requests/{id:guid}/reject"), Authorize(Policy = Policies.LeaveApprove)]
    public async Task<IActionResult> Reject(Guid id, DecisionBody body, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DecideLeaveRequestCommand(id, false, body.Comment), cancellationToken));

    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLeaveCalendarQuery(from, to, departmentId), cancellationToken));

    [HttpGet("employees/{employeeId:guid}")]
    public async Task<IActionResult> Employee(Guid employeeId, [FromQuery] int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEmployeeLeaveQuery(employeeId, year), cancellationToken));

    [HttpPost("employees/{employeeId:guid}/adjustments"), Authorize(Policy = Policies.LeaveAdmin)]
    public async Task<IActionResult> Adjust(Guid employeeId, AdjustmentBody body, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AdjustLeaveBalanceCommand(employeeId, body.LeaveTypeId, body.Year, body.Days, body.Reason), cancellationToken));
}
