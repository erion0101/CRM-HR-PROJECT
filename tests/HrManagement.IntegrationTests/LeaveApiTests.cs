using System.Net;
using System.Text.Json;
using HrManagement.Domain;
using HrManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HrManagement.IntegrationTests;

public sealed partial class ApiAuthorizationTests
{
    [Fact]
    public async Task Time_off_is_requested_by_the_employee_and_decided_only_by_their_manager_or_HR()
    {
        using var admin = Client();
        var adminToken = await LoginAsync(admin, AdminEmail, AdminPassword);
        var managerId = await CreateNamedEmployeeAsync(admin, adminToken, "LV-MGR", "Mira", "Manager");
        var otherManagerId = await CreateNamedEmployeeAsync(admin, adminToken, "LV-MGR2", "Otto", "Otherteam");
        var employeeId = await CreateNamedEmployeeAsync(admin, adminToken, "LV-EMP", "Elena", "Employee");
        var colleagueId = await CreateNamedEmployeeAsync(admin, adminToken, "LV-COL", "Cleo", "Colleague");
        var outsiderId = await CreateNamedEmployeeAsync(admin, adminToken, "LV-OUT", "Oscar", "Outsider");
        foreach (var id in new[] { managerId, otherManagerId, employeeId, colleagueId, outsiderId })
            await SendAsync(admin, HttpMethod.Put, $"/api/v1/employees/{id}/employment",
                new { status = "Active", startDate = "2025-01-01", endDate = (string?)null }, adminToken);
        await AddReportingLineAsync(employeeId, managerId);
        await AddReportingLineAsync(colleagueId, managerId);
        await CreateUserAsync(admin, adminToken, "leave.manager@integration.local", "Manager-Password-123!", "Manager", managerId);
        await CreateUserAsync(admin, adminToken, "leave.other@integration.local", "Manager-Password-123!", "Manager", otherManagerId);
        await CreateUserAsync(admin, adminToken, "leave.employee@integration.local", "Employee-Password-123!", "Employee", employeeId);
        await CreateUserAsync(admin, adminToken, "leave.colleague@integration.local", "Employee-Password-123!", "Employee", colleagueId);
        await CreateUserAsync(admin, adminToken, "leave.outsider@integration.local", "Employee-Password-123!", "Employee", outsiderId);

        using var employee = Client();
        var employeeToken = await LoginAsync(employee, "leave.employee@integration.local", "Employee-Password-123!");
        using var manager = Client();
        var managerToken = await LoginAsync(manager, "leave.manager@integration.local", "Manager-Password-123!");
        using var otherManager = Client();
        var otherManagerToken = await LoginAsync(otherManager, "leave.other@integration.local", "Manager-Password-123!");
        using var colleague = Client();
        await LoginAsync(colleague, "leave.colleague@integration.local", "Employee-Password-123!");
        using var outsider = Client();
        await LoginAsync(outsider, "leave.outsider@integration.local", "Employee-Password-123!");

        // The two company defaults are seeded.
        var types = (await ReadJsonAsync(await employee.GetAsync("/api/v1/leave/types"))).EnumerateArray().ToArray();
        var vacation = types.Single(x => x.GetProperty("name").GetString() == "Vacation");
        var sick = types.Single(x => x.GetProperty("name").GetString() == "Sick leave");
        Assert.Equal(20, vacation.GetProperty("daysPerYear").GetDecimal());
        Assert.Equal(6, vacation.GetProperty("carryOverExpiryMonth").GetInt32());
        Assert.False(sick.GetProperty("carriesOver").GetBoolean());
        var vacationId = vacation.GetProperty("id").GetGuid();

        // A Monday-to-Friday week in March next year: carried-over days are still valid then.
        var year = DateTime.UtcNow.Year + 1;
        var monday = new DateOnly(year, 3, 1);
        while (monday.DayOfWeek != DayOfWeek.Monday) monday = monday.AddDays(1);
        var friday = monday.AddDays(4);

        var preview = await ReadJsonAsync(await employee.GetAsync($"/api/v1/leave/me/preview?leaveTypeId={vacationId}&startDate={monday:yyyy-MM-dd}&endDate={friday:yyyy-MM-dd}"));
        Assert.Equal(5, preview.GetProperty("workingDays").GetInt32());
        Assert.Equal(JsonValueKind.Null, preview.GetProperty("problem").ValueKind);
        // 20 unused days from this year carried over + 5 earned January–March.
        Assert.Equal(25, preview.GetProperty("availableBefore").GetDecimal());
        Assert.Equal(20, preview.GetProperty("availableAfter").GetDecimal());

        var requested = await ReadJsonAsync(await SendAsync(employee, HttpMethod.Post, "/api/v1/leave/requests",
            new { leaveTypeId = vacationId, startDate = monday, endDate = friday, reason = "Family trip" }, employeeToken));
        var requestId = requested.GetProperty("id").GetGuid();
        Assert.Equal("Pending", requested.GetProperty("status").GetString());
        Assert.Equal("Mira Manager", requested.GetProperty("approverName").GetString());

        var overlapping = await SendAsync(employee, HttpMethod.Post, "/api/v1/leave/requests",
            new { leaveTypeId = vacationId, startDate = friday, endDate = friday, reason = (string?)null }, employeeToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, overlapping.StatusCode);

        var tooLong = await SendAsync(employee, HttpMethod.Post, "/api/v1/leave/requests",
            new { leaveTypeId = sick.GetProperty("id").GetGuid(), startDate = new DateOnly(year, 5, 3), endDate = new DateOnly(year, 6, 4), reason = (string?)null },
            employeeToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Contains("Not enough sick leave days", await tooLong.Content.ReadAsStringAsync());

        // Only the employee's own manager (or HR) can see and decide it.
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/v1/leave/approvals")).StatusCode);
        Assert.Empty((await ReadJsonAsync(await otherManager.GetAsync("/api/v1/leave/approvals"))).EnumerateArray());
        var otherDecision = await SendAsync(otherManager, HttpMethod.Post, $"/api/v1/leave/requests/{requestId}/approve", new { comment = (string?)null },
            otherManagerToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.Forbidden, otherDecision.StatusCode);

        var approvals = (await ReadJsonAsync(await manager.GetAsync("/api/v1/leave/approvals"))).EnumerateArray().ToArray();
        Assert.Equal(requestId, Assert.Single(approvals).GetProperty("request").GetProperty("id").GetGuid());
        var managerNotifications = await ReadJsonAsync(await manager.GetAsync("/api/v1/notifications"));
        Assert.Contains(managerNotifications.GetProperty("items").EnumerateArray(), x => x.GetProperty("type").GetString() == "LeaveRequested");

        var rejectWithoutReason = await SendAsync(manager, HttpMethod.Post, $"/api/v1/leave/requests/{requestId}/reject", new { comment = "" },
            managerToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, rejectWithoutReason.StatusCode);
        var approved = await ReadJsonAsync(await SendAsync(manager, HttpMethod.Post, $"/api/v1/leave/requests/{requestId}/approve",
            new { comment = "Enjoy" }, managerToken));
        Assert.Equal("Approved", approved.GetProperty("status").GetString());
        var employeeNotifications = await ReadJsonAsync(await employee.GetAsync("/api/v1/notifications"));
        Assert.Contains(employeeNotifications.GetProperty("items").EnumerateArray(), x => x.GetProperty("type").GetString() == "LeaveApproved");

        // The calendar: the team sees "Out" without the kind of leave; the manager sees the detail; outsiders see nothing.
        var calendarUrl = $"/api/v1/leave/calendar?from={monday:yyyy-MM-dd}&to={friday:yyyy-MM-dd}";
        var colleagueEntry = Assert.Single((await ReadJsonAsync(await colleague.GetAsync(calendarUrl))).GetProperty("entries").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, colleagueEntry.GetProperty("leaveTypeName").ValueKind);
        var managerEntry = Assert.Single((await ReadJsonAsync(await manager.GetAsync(calendarUrl))).GetProperty("entries").EnumerateArray());
        Assert.Equal("Vacation", managerEntry.GetProperty("leaveTypeName").GetString());
        Assert.Empty((await ReadJsonAsync(await outsider.GetAsync(calendarUrl))).GetProperty("entries").EnumerateArray());

        // HR adjusts the balance; cancelling gives the days back.
        await SendAsync(admin, HttpMethod.Post, $"/api/v1/leave/employees/{employeeId}/adjustments",
            new { leaveTypeId = vacationId, year, days = 2, reason = "Agreed in contract" }, adminToken);
        var cancelled = await ReadJsonAsync(await SendAsync(employee, HttpMethod.Post, $"/api/v1/leave/requests/{requestId}/cancel", new { }, employeeToken));
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
        var previewUrl = $"/api/v1/leave/me/preview?leaveTypeId={vacationId}&startDate={monday:yyyy-MM-dd}&endDate={friday:yyyy-MM-dd}";
        Assert.Equal(27, (await ReadJsonAsync(await employee.GetAsync(previewUrl))).GetProperty("availableBefore").GetDecimal());
        var mine = await ReadJsonAsync(await employee.GetAsync($"/api/v1/leave/me?year={year}"));
        Assert.Equal(2, mine.GetProperty("adjustments").EnumerateArray().Single().GetProperty("days").GetDecimal());

        // Leave settings are HR only, and an HR account without an employee profile has no "my time off".
        var forbiddenType = await SendAsync(manager, HttpMethod.Post, "/api/v1/leave/types", new
        {
            name = "Study leave", color = "#16a34a", isPaid = true, requiresApproval = true, deductsFromBalance = true, daysPerYear = 5,
            accrualMethod = "YearlyUpfront", carriesOver = false, maxCarryOverDays = (decimal?)null, carryOverExpiryMonth = (int?)null, carryOverExpiryDay = (int?)null
        }, managerToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenType.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/leave/me")).StatusCode);
    }

    private async Task AddReportingLineAsync(Guid employeeId, Guid managerId)
    {
        using var serviceScope = _factory!.Services.CreateScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var organizationId = await db.Employees.IgnoreQueryFilters().Where(x => x.Id == employeeId).Select(x => x.OrganizationId).SingleAsync();
        db.ReportingLines.Add(ReportingLine.Create(organizationId, employeeId, managerId, new DateOnly(2025, 1, 1), null));
        await db.SaveChangesAsync();
    }
}
