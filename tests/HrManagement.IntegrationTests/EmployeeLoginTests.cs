using System.Net;
using System.Text.Json;
using Xunit;

namespace HrManagement.IntegrationTests;

public sealed partial class ApiAuthorizationTests
{
    [Fact]
    public async Task Each_person_has_at_most_one_login_shown_on_their_profile_and_HR_cannot_lock_themselves_out()
    {
        using var admin = Client();
        var adminToken = await LoginAsync(admin, AdminEmail, AdminPassword);
        var personId = await CreateNamedEmployeeAsync(admin, adminToken, "LOGIN-1", "Lina", "Login");

        var none = await ReadJsonAsync(await admin.GetAsync($"/api/v1/users/by-employee/{personId}"));
        Assert.Equal(JsonValueKind.Null, none.GetProperty("login").ValueKind);

        var userId = await CreateUserAsync(admin, adminToken, "lina.login@integration.local", "Employee-Password-123!", "Employee", personId);
        var found = await ReadJsonAsync(await admin.GetAsync($"/api/v1/users/by-employee/{personId}"));
        Assert.Equal(userId, found.GetProperty("login").GetProperty("id").GetGuid());

        var second = await SendAsync(admin, HttpMethod.Post, "/api/v1/users",
            new { email = "lina.second@integration.local", temporaryPassword = "Employee-Password-123!", role = "Employee", employeeId = personId },
            adminToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        // Promote to Manager from the profile, then turn the login off.
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/users/{userId}", new { isActive = true, role = "Manager", employeeId = personId }, adminToken);
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/users/{userId}", new { isActive = false, role = "Manager", employeeId = personId }, adminToken);
        using var lina = Client();
        var login = await lina.PostAsync("/api/v1/auth/login", System.Net.Http.Json.JsonContent.Create(
            new { email = "lina.login@integration.local", password = "Employee-Password-123!", rememberMe = false }));
        Assert.False(login.IsSuccessStatusCode);

        // The admin account is not a person, so it appears in the "not linked" list, and it cannot switch itself off.
        var unlinked = (await ReadJsonAsync(await admin.GetAsync("/api/v1/users?unlinkedOnly=true"))).GetProperty("items").EnumerateArray().ToArray();
        Assert.All(unlinked, x => Assert.Equal(JsonValueKind.Null, x.GetProperty("employeeId").ValueKind));
        var adminUser = unlinked.Single(x => x.GetProperty("email").GetString() == AdminEmail);
        var lockout = await SendAsync(admin, HttpMethod.Put, $"/api/v1/users/{adminUser.GetProperty("id").GetGuid()}",
            new { isActive = false, role = "HR", employeeId = (Guid?)null }, adminToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, lockout.StatusCode);
        var demote = await SendAsync(admin, HttpMethod.Put, $"/api/v1/users/{adminUser.GetProperty("id").GetGuid()}",
            new { isActive = true, role = "Employee", employeeId = (Guid?)null }, adminToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, demote.StatusCode);
    }
}
