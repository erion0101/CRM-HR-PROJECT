using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HrManagement.Application.Features.Ai;
using HrManagement.Application.Features.Notifications;
using HrManagement.Domain;
using HrManagement.Infrastructure;
using HrManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace HrManagement.IntegrationTests;

public sealed class ApiAuthorizationTests : IAsyncLifetime
{
    private const string AdminEmail = "admin@integration.local";
    private const string AdminPassword = "Integration-Admin-Password-123!";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("hr_api_tests").WithUsername("hr_test").WithPassword("hr_test_password").Build();
    private string _storage = string.Empty;
    private WebApplicationFactory<global::Program>? _factory;
    private readonly Dictionary<string, string?> _previousEnvironment = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _storage = Path.Combine(Path.GetTempPath(), $"hr-api-tests-{Guid.NewGuid():N}");
        SetEnvironment("ConnectionStrings__Postgres", _postgres.GetConnectionString());
        SetEnvironment("ConnectionStrings__Redis", "127.0.0.1:1,abortConnect=false,connectTimeout=100");
        SetEnvironment("Storage__RootPath", _storage);
        SetEnvironment("Seed__OrganizationName", "Integration Organization");
        SetEnvironment("Seed__AdminEmail", AdminEmail);
        SetEnvironment("Seed__AdminPassword", AdminPassword);
        SetEnvironment("Ai__Enabled", "true");
        SetEnvironment("Ai__RealDataApproved", "true");
        SetEnvironment("Ai__InputCostPerMillionUsd", "1");
        SetEnvironment("Ai__OutputCostPerMillionUsd", "1");
        SetEnvironment("OPENAI_API_KEY", "integration-test-key");
        _factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiProvider>();
                services.RemoveAll<IEmbeddingProvider>();
                services.RemoveAll<IAiUsageService>();
                services.AddSingleton<IAiProvider, FakeAiProvider>();
                services.AddSingleton<IEmbeddingProvider, FakeEmbeddingProvider>();
                services.AddScoped<IAiUsageService, FakeAiUsageService>();
            });
        });
        _ = _factory.Server;
        await _factory.Services.InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        if (Directory.Exists(_storage)) Directory.Delete(_storage, recursive: true);
        foreach (var item in _previousEnvironment) Environment.SetEnvironmentVariable(item.Key, item.Value);
    }

    [Fact]
    public async Task Role_scopes_duplicates_and_etags_are_enforced()
    {
        using var admin = Client();
        var adminToken = await LoginAsync(admin, AdminEmail, AdminPassword);

        var businessUnit = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "Product and Engineering", code = "PENG", type = "BusinessUnit", parentDepartmentId = (Guid?)null
        }, adminToken));
        var division = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "Engineering", code = "ENG", type = "Division", parentDepartmentId = businessUnit.GetProperty("id").GetGuid()
        }, adminToken));
        var manager = await CreateEmployeeAsync(admin, adminToken, "MGR-1", "manager.person@integration.local");
        var department = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "IT", code = "IT-DEPT", type = "Department", parentDepartmentId = division.GetProperty("id").GetGuid(), managerEmployeeId = manager.Id
        }, adminToken));
        var team = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "Applications", code = "APP", type = "Team", parentDepartmentId = department.GetProperty("id").GetGuid()
        }, adminToken));
        Assert.Equal(department.GetProperty("id").GetGuid(), team.GetProperty("parentDepartmentId").GetGuid());

        var invalidHierarchy = await SendAsync(admin, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "Invalid parent order", code = "INVALID", type = "BusinessUnit", parentDepartmentId = division.GetProperty("id").GetGuid()
        }, adminToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, invalidHierarchy.StatusCode);

        var directReport = await CreateEmployeeAsync(admin, adminToken, "EMP-1", "direct.report@integration.local");
        var outsider = await CreateEmployeeAsync(admin, adminToken, "EMP-2", "outside.person@integration.local");
        var hrEmployee = await CreateEmployeeAsync(admin, adminToken, "HR-1", "hr.person@integration.local");

        var position = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/positions", new
        {
            departmentId = department.GetProperty("id").GetGuid(), title = "Software Engineer", code = "SWE"
        }, adminToken));
        await SendAsync(admin, HttpMethod.Post, $"/api/v1/employees/{directReport.Id}/assignments", new
        {
            positionId = position.GetProperty("id").GetGuid(), departmentId = department.GetProperty("id").GetGuid(), effectiveFrom = "2026-01-01", effectiveTo = (string?)null
        }, adminToken);

        await SendAsync(admin, HttpMethod.Put, $"/api/v1/employees/{directReport.Id}/employment", new
        {
            status = "Active", startDate = "2026-01-02", endDate = (string?)null
        }, adminToken);
        await CreateUserAsync(admin, adminToken, "manager@integration.local", "Manager-Password-123!", "Manager", manager.Id);
        await CreateUserAsync(admin, adminToken, "employee@integration.local", "Employee-Password-123!", "Employee", outsider.Id);
        var hrUserId = await CreateUserAsync(admin, adminToken, "hr@integration.local", "HR-Password-123!", "HR", hrEmployee.Id);
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/users/{hrUserId}/scopes", new
        {
            scopes = new[] { new { scopeType = "Department", scopeEntityId = businessUnit.GetProperty("id").GetGuid() } }
        }, adminToken);

        var duplicate = await SendAsync(admin, HttpMethod.Post, "/api/v1/employees", new
        {
            employeeNumber = "EMP-1", firstName = "Duplicate", lastName = "Person", workEmail = "duplicate@integration.local", phone = (string?)null
        }, adminToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var staleUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/employees/{directReport.Id}")
        {
            Content = JsonContent.Create(new { firstName = "Changed", lastName = "Name", workEmail = "changed@integration.local", phone = (string?)null })
        };
        staleUpdate.Headers.Add("X-CSRF-TOKEN", adminToken);
        staleUpdate.Headers.TryAddWithoutValidation("If-Match", "\"999\"");
        var staleResponse = await admin.SendAsync(staleUpdate);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        var adminDashboard = await ReadJsonAsync(await admin.GetAsync("/api/v1/dashboard/summary"));
        Assert.Equal(4, adminDashboard.GetProperty("visibleEmployees").GetInt32());
        Assert.Equal(1, adminDashboard.GetProperty("activeEmployees").GetInt32());
        Assert.Equal(3, adminDashboard.GetProperty("setupRequiredEmployees").GetInt32());

        using var managerClient = Client();
        var managerToken = await LoginAsync(managerClient, "manager@integration.local", "Manager-Password-123!");
        Assert.Equal(HttpStatusCode.OK, (await managerClient.GetAsync($"/api/v1/employees/{directReport.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await managerClient.GetAsync($"/api/v1/employees/{outsider.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await managerClient.GetAsync($"/api/v1/employees/{directReport.Id}/compensation")).StatusCode);

        var profile = await ReadJsonAsync(await managerClient.GetAsync($"/api/v1/employees/{directReport.Id}/profile"));
        Assert.Equal("2026-01-02", profile.GetProperty("hireDate").GetString());
        Assert.Equal(manager.Id, profile.GetProperty("manager").GetProperty("id").GetGuid());
        var organizationPath = profile.GetProperty("organizationPath").EnumerateArray().ToArray();
        Assert.Equal(3, organizationPath.Length);
        Assert.Equal("Product and Engineering", organizationPath[0].GetProperty("name").GetString());
        Assert.Equal("IT", organizationPath[2].GetProperty("name").GetString());

        var directReports = await ReadJsonAsync(await managerClient.GetAsync($"/api/v1/employees/{manager.Id}/direct-reports"));
        Assert.Contains(directReports.EnumerateArray(), item => item.GetProperty("id").GetGuid() == directReport.Id);

        var reportingLines = await ReadJsonAsync(await managerClient.GetAsync($"/api/v1/employees/{directReport.Id}/reporting-lines"));
        Assert.Contains(reportingLines.EnumerateArray(), item => item.GetProperty("manager").GetProperty("id").GetGuid() == manager.Id);

        var managerDashboard = await ReadJsonAsync(await managerClient.GetAsync("/api/v1/dashboard/summary"));
        Assert.Equal(2, managerDashboard.GetProperty("visibleEmployees").GetInt32());
        Assert.Equal(1, managerDashboard.GetProperty("activeEmployees").GetInt32());
        Assert.Equal(1, managerDashboard.GetProperty("activeOrganizationUnits").GetInt32());

        var activity = await ReadJsonAsync(await managerClient.GetAsync($"/api/v1/employees/{directReport.Id}/activity"));
        Assert.NotEmpty(activity.GetProperty("items").EnumerateArray());
        Assert.DoesNotContain(activity.GetProperty("items").EnumerateArray(), item => item.GetProperty("entityType").GetString() == "CompensationRecord");
        Assert.Equal(HttpStatusCode.NotFound, (await managerClient.GetAsync($"/api/v1/employees/{outsider.Id}/activity")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await managerClient.GetAsync($"/api/v1/employees/{directReport.Id}/documents")).StatusCode);
        var managerUpload = await SendAsync(managerClient, HttpMethod.Post, $"/api/v1/employees/{directReport.Id}/documents", new { }, managerToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.Forbidden, managerUpload.StatusCode);

        using var employeeClient = Client();
        var employeeToken = await LoginAsync(employeeClient, "employee@integration.local", "Employee-Password-123!");
        Assert.Equal(HttpStatusCode.OK, (await employeeClient.GetAsync($"/api/v1/employees/{outsider.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await employeeClient.GetAsync($"/api/v1/employees/{directReport.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await employeeClient.GetAsync($"/api/v1/employees/{outsider.Id}/activity")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await employeeClient.GetAsync($"/api/v1/employees/{outsider.Id}/documents")).StatusCode);
        using var employeeUploadRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/employees/{outsider.Id}/documents");
        using var employeeUploadContent = new MultipartFormDataContent();
        var employeeFile = new ByteArrayContent("%PDF-1.7\n%%EOF"u8.ToArray());
        employeeFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        employeeUploadContent.Add(employeeFile, "file", "employee-upload.pdf");
        employeeUploadContent.Add(new StringContent("Employee Upload"), "documentType");
        employeeUploadRequest.Content = employeeUploadContent;
        employeeUploadRequest.Headers.Add("X-CSRF-TOKEN", employeeToken);
        var employeeUpload = await employeeClient.SendAsync(employeeUploadRequest);
        Assert.Equal(HttpStatusCode.Created, employeeUpload.StatusCode);

        using var hrClient = Client();
        _ = await LoginAsync(hrClient, "hr@integration.local", "HR-Password-123!");
        Assert.Equal(HttpStatusCode.OK, (await hrClient.GetAsync($"/api/v1/employees/{directReport.Id}/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await hrClient.GetAsync($"/api/v1/employees/{manager.Id}")).StatusCode);
    }

    [Fact]
    public async Task Department_overview_exposes_unique_leadership_and_prevents_a_third_department()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var manager = await CreateEmployeeAsync(client, token, "LEAD-1", "department.manager@integration.local");
        var hr = await CreateEmployeeAsync(client, token, "LEAD-2", "department.hr@integration.local");
        var productManager = await CreateEmployeeAsync(client, token, "LEAD-3", "department.product@integration.local");

        var department = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "Technology",
            code = "TECH",
            type = "Department",
            parentDepartmentId = (Guid?)null,
            managerEmployeeId = manager.Id,
            hrEmployeeId = hr.Id,
            productManagerEmployeeId = productManager.Id,
            description = "Product and development"
        }, token));

        var overview = await ReadJsonAsync(await client.GetAsync($"/api/v1/departments/current?departmentId={department.GetProperty("id").GetGuid()}"));
        Assert.Equal(department.GetProperty("id").GetGuid(), overview.GetProperty("department").GetProperty("id").GetGuid());
        Assert.Equal(manager.Id, overview.GetProperty("department").GetProperty("managerEmployeeId").GetGuid());
        Assert.Equal(hr.Id, overview.GetProperty("department").GetProperty("hrEmployeeId").GetGuid());
        Assert.Equal(productManager.Id, overview.GetProperty("department").GetProperty("productManagerEmployeeId").GetGuid());

        var secondResponse = await SendAsync(client, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "HR",
            code = "HR-DEPT",
            type = "Department",
            managerEmployeeId = hr.Id
        }, token);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = await ReadJsonAsync(secondResponse);
        var hrPosition = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/positions", new
        {
            departmentId = second.GetProperty("id").GetGuid(),
            title = "Senior HR",
            code = "HR-SENIOR"
        }, token));
        var mismatchedAssignment = await SendAsync(client, HttpMethod.Post, $"/api/v1/employees/{manager.Id}/assignments", new
        {
            positionId = hrPosition.GetProperty("id").GetGuid(),
            departmentId = department.GetProperty("id").GetGuid(),
            effectiveFrom = "2026-01-01",
            effectiveTo = (string?)null
        }, token, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, mismatchedAssignment.StatusCode);

        var itPosition = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/positions", new
        {
            departmentId = department.GetProperty("id").GetGuid(),
            title = "IT Specialist",
            code = "IT-SPECIALIST"
        }, token));
        await SendAsync(client, HttpMethod.Post, $"/api/v1/employees/{productManager.Id}/assignments", new
        {
            positionId = itPosition.GetProperty("id").GetGuid(),
            departmentId = department.GetProperty("id").GetGuid(),
            effectiveFrom = "2026-01-01"
        }, token);
        await SendAsync(client, HttpMethod.Post, $"/api/v1/employees/{productManager.Id}/assignments", new
        {
            positionId = hrPosition.GetProperty("id").GetGuid(),
            departmentId = second.GetProperty("id").GetGuid(),
            effectiveFrom = "2026-02-01"
        }, token);

        var transferredProfile = await ReadJsonAsync(await client.GetAsync($"/api/v1/employees/{productManager.Id}/profile"));
        Assert.Equal("HR", transferredProfile.GetProperty("department").GetProperty("name").GetString());
        Assert.Equal("Senior HR", transferredProfile.GetProperty("position").GetProperty("name").GetString());
        Assert.Equal(hr.Id, transferredProfile.GetProperty("manager").GetProperty("id").GetGuid());
        var assignmentHistory = await ReadJsonAsync(await client.GetAsync($"/api/v1/employees/{productManager.Id}/assignments"));
        Assert.Equal(2, assignmentHistory.GetArrayLength());

        var third = await SendAsync(client, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "Third department",
            code = "THIRD",
            type = "Department"
        }, token, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
    }

    [Fact]
    public async Task Ai_conversation_uses_fake_provider_without_external_calls()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var capabilities = await ReadJsonAsync(await client.GetAsync("/api/v1/ai/capabilities"));
        Assert.True(capabilities.GetProperty("enabled").GetBoolean());

        var created = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/ai/conversations",
            new { title = "Integration AI", contextType = (string?)null, contextId = (Guid?)null }, token));
        var conversationId = created.GetProperty("id").GetGuid();
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, $"/api/v1/ai/conversations/{conversationId}/messages",
            new { message = "What data can I access?" }, token));

        Assert.Equal("Completed", response.GetProperty("status").GetString());
        Assert.Equal("No authorized evidence was requested.", response.GetProperty("message").GetString());
        Assert.Equal("NotFound", response.GetProperty("evidenceStatus").GetString());
    }

    [Fact]
    public async Task Ai_resolves_a_name_then_reads_an_authorized_structured_birthday()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        await SendAsync(client, HttpMethod.Post, "/api/v1/employees", new
        {
            employeeNumber = $"AI-{Guid.NewGuid():N}"[..12],
            firstName = "Erion",
            lastName = "Assistant Test",
            workEmail = $"erion.{Guid.NewGuid():N}@integration.local",
            phone = (string?)null,
            dateOfBirth = "1994-06-18"
        }, token);

        var created = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/ai/conversations",
            new { title = "Birthday", contextType = (string?)null, contextId = (Guid?)null }, token));
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{created.GetProperty("id").GetGuid()}/messages",
            new { message = "What is Erion's birthday?" }, token));

        Assert.Equal("Completed", response.GetProperty("status").GetString());
        Assert.Contains("1994-06-18", response.GetProperty("message").GetString());
        Assert.Equal("VerifiedStructured", response.GetProperty("evidenceStatus").GetString());
        Assert.Single(response.GetProperty("sources").EnumerateArray());
    }

    [Fact]
    public async Task Ai_returns_an_authorized_general_employee_profile()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        await SendAsync(client, HttpMethod.Post, "/api/v1/employees", new
        {
            employeeNumber = "ERI-PROFILE",
            firstName = "Erion",
            lastName = "Profile",
            workEmail = "erion.profile@integration.local",
            phone = (string?)null,
            dateOfBirth = (string?)null
        }, token);
        using (var serviceScope = _factory!.Services.CreateScope())
        {
            var db = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherOrganization = Organization.Create($"Other organization {Guid.NewGuid():N}");
            db.Organizations.Add(otherOrganization);
            db.Employees.Add(Employee.Create(otherOrganization.Id, "FOREIGN-ERI", "Erion", "Foreign",
                $"foreign.{Guid.NewGuid():N}@integration.local", null));
            await db.SaveChangesAsync();
        }
        var conversation = await CreateAiConversationAsync(client, token, "Employee profile");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "Give me information about Erion." }, token));

        Assert.Equal("VerifiedStructured", response.GetProperty("evidenceStatus").GetString());
        Assert.Contains("erion.profile@integration.local", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Ai_resolves_an_employee_and_returns_the_current_manager()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var manager = await CreateNamedEmployeeAsync(client, token, "MGR-AI", "John", "Manager");
        var employee = await CreateNamedEmployeeAsync(client, token, "ERI-AI", "Erion", "Reports");
        var department = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "IT", code = $"IT-{Guid.NewGuid():N}"[..12], type = "Department", managerEmployeeId = manager
        }, token));
        var position = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/positions", new
        {
            departmentId = department.GetProperty("id").GetGuid(), title = "Software Engineer", code = $"SWE-{Guid.NewGuid():N}"[..12]
        }, token));
        await SendAsync(client, HttpMethod.Post, $"/api/v1/employees/{employee}/assignments", new
        {
            departmentId = department.GetProperty("id").GetGuid(), positionId = position.GetProperty("id").GetGuid(), effectiveFrom = "2026-01-01", effectiveTo = (string?)null
        }, token);

        var conversation = await CreateAiConversationAsync(client, token, "Manager lookup");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "Who is Erion's manager?" }, token));

        Assert.Equal("VerifiedStructured", response.GetProperty("evidenceStatus").GetString());
        Assert.Contains("John Manager", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Ai_uses_structured_contract_search_for_the_current_month()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var employee = await CreateNamedEmployeeAsync(client, token, "CON-AI", "Contract", "Employee");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        var first = new DateOnly(today.Year, today.Month, 1);
        var next = first.AddMonths(1);
        var create = await SendAsync(client, HttpMethod.Post, $"/api/v1/employees/{employee}/contracts", new
        {
            contractNumber = $"AI-{Guid.NewGuid():N}"[..15],
            startDate = first.AddMonths(-1).ToString("yyyy-MM-dd"),
            endDate = next.AddDays(-1).ToString("yyyy-MM-dd"),
            notes = (string?)null
        }, token);
        var contract = await ReadJsonAsync(create);
        await SendWithEtagAsync(client, HttpMethod.Post,
            $"/api/v1/employees/{employee}/contracts/{contract.GetProperty("id").GetGuid()}/activate",
            new { }, token, contract.GetProperty("version").GetInt64());

        var conversation = await CreateAiConversationAsync(client, token, "Expiring contracts");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "Give me all employees whose contracts expire this month." }, token));

        Assert.Equal("ContractList", response.GetProperty("result").GetProperty("type").GetString());
        Assert.Equal("VerifiedStructured", response.GetProperty("evidenceStatus").GetString());
        Assert.Contains(response.GetProperty("sources").EnumerateArray(), source =>
            source.GetProperty("entityType").GetString() == "Contract");
    }

    [Fact]
    public async Task Ai_does_not_invent_an_unstored_favorite_food()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var conversation = await CreateAiConversationAsync(client, token, "Missing information");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "What is Erion's favorite food?" }, token));

        Assert.Equal("The HR system does not contain available evidence for that information.", response.GetProperty("message").GetString());
        Assert.Equal("NotFound", response.GetProperty("evidenceStatus").GetString());
        Assert.Empty(response.GetProperty("sources").EnumerateArray());
    }

    [Fact]
    public async Task Ai_asks_for_clarification_when_a_name_is_ambiguous()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        await CreateNamedEmployeeAsync(client, token, "JOHN-1", "John", "One");
        await CreateNamedEmployeeAsync(client, token, "JOHN-2", "John", "Two");
        var conversation = await CreateAiConversationAsync(client, token, "Ambiguous employee");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "Give me John's birthday." }, token));

        Assert.Contains("clarify", response.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, response.GetProperty("sources").GetArrayLength());
    }

    [Fact]
    public async Task Ai_uses_authorized_document_rag_for_a_notice_period()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var employee = await CreateNamedEmployeeAsync(client, token, "DOC-AI", "Erion", "Contract");
        await SeedIndexedDocumentAsync(employee, "Employment Contract", "Erion Employment Contract",
            "The employee must provide 30 calendar days notice before terminating employment.", page: 7);

        var conversation = await CreateAiConversationAsync(client, token, "Contract notice");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "What does Erion's contract say about notice period?" }, token));

        Assert.Equal("DocumentDerived", response.GetProperty("evidenceStatus").GetString());
        Assert.Contains("30 calendar days", response.GetProperty("message").GetString());
        Assert.Contains(response.GetProperty("sources").EnumerateArray(), source =>
            source.GetProperty("kind").GetString() == "DocumentChunk" && source.GetProperty("location").GetString() == "page 7");
    }

    [Fact]
    public async Task Ai_resolves_document_experience_to_an_authorized_employee()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var employee = await CreateNamedEmployeeAsync(client, token, "CV-AI", "Erion", "Mobile");
        await SeedIndexedDocumentAsync(employee, "CV", "Erion CV",
            "Built production mobile applications with React Native and TypeScript.", page: 2);

        var conversation = await CreateAiConversationAsync(client, token, "CV skills");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "Which employees have React Native experience according to their CVs?" }, token));

        Assert.Equal("Mixed", response.GetProperty("evidenceStatus").GetString());
        Assert.Contains("Erion Mobile", response.GetProperty("message").GetString());
        Assert.Equal(2, response.GetProperty("sources").GetArrayLength());
    }

    [Fact]
    public async Task Ai_combines_employee_contract_and_document_tools_for_a_hybrid_question()
    {
        using var client = Client();
        var token = await LoginAsync(client, AdminEmail, AdminPassword);
        var employee = await CreateNamedEmployeeAsync(client, token, "HYB-AI", "Erion", "Hybrid");
        var unit = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/departments", new
        {
            name = "Engineering Hybrid",
            code = $"ENG-{Guid.NewGuid():N}"[..12],
            type = "Department",
            parentDepartmentId = (Guid?)null
        }, token));
        var position = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/positions", new
        {
            departmentId = unit.GetProperty("id").GetGuid(),
            title = "Software Engineer",
            code = $"SWE-{Guid.NewGuid():N}"[..12]
        }, token));
        await SendAsync(client, HttpMethod.Post, $"/api/v1/employees/{employee}/assignments", new
        {
            positionId = position.GetProperty("id").GetGuid(),
            departmentId = unit.GetProperty("id").GetGuid(),
            effectiveFrom = $"{DateTimeOffset.UtcNow.Year}-01-01",
            effectiveTo = (string?)null
        }, token);
        var contractCreate = await SendAsync(client, HttpMethod.Post, $"/api/v1/employees/{employee}/contracts", new
        {
            contractNumber = $"HY-{Guid.NewGuid():N}"[..15],
            startDate = $"{DateTimeOffset.UtcNow.Year}-01-01",
            endDate = $"{DateTimeOffset.UtcNow.Year}-12-31",
            notes = (string?)null
        }, token);
        var contract = await ReadJsonAsync(contractCreate);
        await SendWithEtagAsync(client, HttpMethod.Post,
            $"/api/v1/employees/{employee}/contracts/{contract.GetProperty("id").GetGuid()}/activate",
            new { }, token, contract.GetProperty("version").GetInt64());
        await SeedIndexedDocumentAsync(employee, "CV", "Erion Hybrid CV",
            "Professional experience includes React Native mobile product development.", page: 3);

        var conversation = await CreateAiConversationAsync(client, token, "Hybrid query");
        var response = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post,
            $"/api/v1/ai/conversations/{conversation}/messages",
            new { message = "Give me Software Engineers whose contracts expire this year and whose CVs mention React Native." }, token));

        Assert.Equal("Mixed", response.GetProperty("evidenceStatus").GetString());
        Assert.Contains("Erion Hybrid", response.GetProperty("message").GetString());
        Assert.Equal(3, response.GetProperty("sources").GetArrayLength());
    }

    private HttpClient Client() => _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private void SetEnvironment(string name, string value)
    {
        _previousEnvironment[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    [Fact]
    public async Task Activating_employment_assigns_the_default_company_package_once()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var package = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/templates", new
        {
            name = "Company onboarding", description = "How the company works", durationDays = 30, kind = "Company",
            steps = new[]
            {
                new { sectionTitle = "Welcome", sectionOrder = 0, title = "Meet the team", instructions = "Say hello",
                    expectedOutcome = "Know the team", stepType = "PracticalTask", stepOrder = 0, dueAfterDays = 1, isRequired = true }
            }
        }, token));
        var packageId = package.GetProperty("id").GetGuid();
        var published = await ReadJsonAsync(await SendWithEtagAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/templates/{packageId}/publish",
            new { }, token, package.GetProperty("version").GetInt64()));
        var marked = await ReadJsonAsync(await SendWithEtagAsync(admin, HttpMethod.Put, $"/api/v1/onboarding/templates/{packageId}/auto-assign",
            new { enabled = true }, token, published.GetProperty("version").GetInt64()));
        Assert.True(marked.GetProperty("autoAssignToNewEmployees").GetBoolean());

        var hire = await CreateEmployeeAsync(admin, token, "NEW-1", "new.hire@integration.local");
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/employees/{hire.Id}/employment",
            new { status = "Active", startDate = "2026-11-10", endDate = (string?)null }, token);
        // A later change to an already active employment must not create a second journey.
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/employees/{hire.Id}/employment",
            new { status = "Active", startDate = "2026-11-11", endDate = (string?)null }, token);

        var assignments = await ReadJsonAsync(await admin.GetAsync($"/api/v1/onboarding/assignments?employeeId={hire.Id}"));
        var journey = Assert.Single(assignments.EnumerateArray());
        Assert.Equal(packageId, journey.GetProperty("templateId").GetGuid());
        Assert.Equal("2026-11-10", journey.GetProperty("startDate").GetString());

        var draftEmployee = await CreateEmployeeAsync(admin, token, "NEW-2", "draft.hire@integration.local");
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/employees/{draftEmployee.Id}/employment",
            new { status = "Draft", startDate = "2026-11-10", endDate = (string?)null }, token);
        var none = await ReadJsonAsync(await admin.GetAsync($"/api/v1/onboarding/assignments?employeeId={draftEmployee.Id}"));
        Assert.Empty(none.EnumerateArray());
    }

    [Fact]
    public async Task Joining_a_project_adds_its_package_to_a_new_hire_or_as_a_separate_journey()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var lead = await CreateEmployeeAsync(admin, token, "LEAD-1", "project.lead@integration.local");
        var project = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/projects", new
        {
            code = "PORTAL", name = "Client Portal", purpose = "Customer self-service", description = "Portal project",
            departmentId = (Guid?)null, leadEmployeeId = lead.Id, onboardingContactEmployeeId = lead.Id, status = "Active",
            links = Array.Empty<object>()
        }, token));
        var projectId = project.GetProperty("id").GetGuid();

        // An existing employee activated before any default package exists has no journey yet.
        var existing = await CreateEmployeeAsync(admin, token, "OLD-1", "existing.person@integration.local");
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/employees/{existing.Id}/employment",
            new { status = "Active", startDate = "2026-01-05", endDate = (string?)null }, token);

        var company = await CreatePublishedPackageAsync(admin, token, "Company onboarding", "Company", null);
        await SendWithEtagAsync(admin, HttpMethod.Put, $"/api/v1/onboarding/templates/{company.Id}/auto-assign",
            new { enabled = true }, token, company.Version);
        var portal = await CreatePublishedPackageAsync(admin, token, "Client Portal onboarding", "Project", projectId);

        var hire = await CreateEmployeeAsync(admin, token, "NEW-3", "portal.hire@integration.local");
        await SendAsync(admin, HttpMethod.Put, $"/api/v1/employees/{hire.Id}/employment",
            new { status = "Active", startDate = "2026-11-10", endDate = (string?)null }, token);
        await SendAsync(admin, HttpMethod.Post, $"/api/v1/projects/{projectId}/members", new
        {
            employeeId = hire.Id, projectRole = "Developer", responsibilities = "Build features",
            effectiveFrom = "2026-11-10", effectiveTo = (string?)null
        }, token);

        // The not-yet-started company journey is replaced by one combined company + project journey.
        var hireJourneys = await ActiveJourneysAsync(admin, hire.Id);
        var combined = Assert.Single(hireJourneys);
        Assert.Equal(company.Id, combined.GetProperty("templateId").GetGuid());
        Assert.Equal(portal.Id, combined.GetProperty("projectTemplateId").GetGuid());
        Assert.Equal(lead.Id, combined.GetProperty("mentorEmployeeId").GetGuid());
        Assert.Equal(2, combined.GetProperty("steps").GetArrayLength());

        await SendAsync(admin, HttpMethod.Post, $"/api/v1/projects/{projectId}/members", new
        {
            employeeId = existing.Id, projectRole = "Tester", responsibilities = "Test features",
            effectiveFrom = "2026-11-10", effectiveTo = (string?)null
        }, token);

        // Someone without a combinable journey gets the project package as its own journey.
        var separate = Assert.Single(await ActiveJourneysAsync(admin, existing.Id));
        Assert.Equal(portal.Id, separate.GetProperty("templateId").GetGuid());
        Assert.Equal(projectId, separate.GetProperty("projectId").GetGuid());
    }

    [Fact]
    public async Task Onboarding_insights_flag_overdue_and_unstarted_journeys()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var empty = await ReadJsonAsync(await admin.GetAsync("/api/v1/onboarding/insights"));
        Assert.Equal(0, empty.GetProperty("activeJourneys").GetInt32());
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("firstAttemptPassRate").ValueKind);

        var package = await CreatePublishedPackageAsync(admin, token, "Company onboarding", "Company", null);
        var late = await CreateEmployeeAsync(admin, token, "LATE-1", "late.starter@integration.local");
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/assignments", new
        {
            employeeId = late.Id, templateId = package.Id, projectId = (Guid?)null, managerEmployeeId = (Guid?)null,
            mentorEmployeeId = (Guid?)null, startDate
        }, token);

        var insights = await ReadJsonAsync(await admin.GetAsync("/api/v1/onboarding/insights"));

        Assert.Equal(1, insights.GetProperty("activeJourneys").GetInt32());
        Assert.Equal(1, insights.GetProperty("overdueJourneys").GetInt32());
        var item = Assert.Single(insights.GetProperty("needsAttention").EnumerateArray());
        Assert.Equal(late.Id, item.GetProperty("employeeId").GetGuid());
        Assert.Equal(1, item.GetProperty("overdueSteps").GetInt32());
        var reasons = item.GetProperty("reasons").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("OverdueSteps", reasons);
        Assert.Contains("NotStarted", reasons);
        Assert.Empty(insights.GetProperty("documentDifficulty").EnumerateArray());
    }

    [Fact]
    public async Task Onboarding_events_notify_the_employee_and_their_reviewer_privately()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var package = await CreatePublishedPackageAsync(admin, token, "Company onboarding", "Company", null);
        var newcomer = await CreateEmployeeAsync(admin, token, "NOTE-1", "newcomer@integration.local");
        var reviewer = await CreateEmployeeAsync(admin, token, "NOTE-2", "reviewer@integration.local");
        await CreateUserAsync(admin, token, "newcomer.user@integration.local", "Newcomer-Password-123!", "Employee", newcomer.Id);
        await CreateUserAsync(admin, token, "reviewer.user@integration.local", "Reviewer-Password-123!", "Manager", reviewer.Id);
        await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/assignments", new
        {
            employeeId = newcomer.Id, templateId = package.Id, projectId = (Guid?)null, managerEmployeeId = reviewer.Id,
            mentorEmployeeId = (Guid?)null, startDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        }, token);

        using var employee = Client();
        var employeeToken = await LoginAsync(employee, "newcomer.user@integration.local", "Newcomer-Password-123!");
        var assigned = await ReadJsonAsync(await employee.GetAsync("/api/v1/notifications"));
        Assert.Equal(1, assigned.GetProperty("unreadCount").GetInt32());
        Assert.Equal("JourneyAssigned", assigned.GetProperty("items")[0].GetProperty("type").GetString());

        // Completing the only required step completes the journey.
        var journey = (await ReadJsonAsync(await employee.GetAsync("/api/v1/onboarding/me"))).GetProperty("journeys")[0];
        var step = journey.GetProperty("steps")[0];
        await SendWithEtagAsync(employee, HttpMethod.Put,
            $"/api/v1/onboarding/assignments/{journey.GetProperty("id").GetGuid()}/steps/{step.GetProperty("id").GetGuid()}",
            new { status = "Completed", completionNote = (string?)null }, employeeToken, step.GetProperty("version").GetInt64());

        var mine = await ReadJsonAsync(await employee.GetAsync("/api/v1/notifications"));
        Assert.Equal(2, mine.GetProperty("unreadCount").GetInt32());
        Assert.Equal("JourneyCompleted", mine.GetProperty("items")[0].GetProperty("type").GetString());

        using var manager = Client();
        var managerToken = await LoginAsync(manager, "reviewer.user@integration.local", "Reviewer-Password-123!");
        var managerNotes = await ReadJsonAsync(await manager.GetAsync("/api/v1/notifications"));
        var completed = Assert.Single(managerNotes.GetProperty("items").EnumerateArray());
        Assert.Equal("JourneyCompleted", completed.GetProperty("type").GetString());
        Assert.Contains("NOTE-1", completed.GetProperty("title").GetString());

        // Someone else's notification is invisible, even by id.
        var othersId = mine.GetProperty("items")[0].GetProperty("id").GetGuid();
        var forbidden = await SendAsync(manager, HttpMethod.Post, $"/api/v1/notifications/{othersId}/read", new { }, managerToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);

        await SendAsync(employee, HttpMethod.Post, "/api/v1/notifications/read-all", new { }, employeeToken);
        Assert.Equal(0, (await ReadJsonAsync(await employee.GetAsync("/api/v1/notifications"))).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Overdue_steps_are_reminded_once_to_employee_and_reviewer()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var package = await CreatePublishedPackageAsync(admin, token, "Company onboarding", "Company", null);
        var late = await CreateEmployeeAsync(admin, token, "DUE-1", "late.person@integration.local");
        var reviewer = await CreateEmployeeAsync(admin, token, "DUE-2", "due.reviewer@integration.local");
        await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/assignments", new
        {
            employeeId = late.Id, templateId = package.Id, projectId = (Guid?)null, managerEmployeeId = reviewer.Id,
            mentorEmployeeId = (Guid?)null, startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10).ToString("yyyy-MM-dd")
        }, token);

        await using (var scope = _factory!.Services.CreateAsyncScope())
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<HrManagement.Application.Features.Onboarding.IOnboardingOverdueReminderProcessor>()
                .ProcessAsync(CancellationToken.None));

        await using var check = _factory.Services.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var reminders = await db.Notifications.Where(x => x.Type == NotificationType.StepOverdue).ToListAsync();
        Assert.Equal(2, reminders.Count);
        Assert.Contains(reminders, x => x.RecipientEmployeeId == late.Id && x.LinkPath == "/onboarding");
        Assert.Contains(reminders, x => x.RecipientEmployeeId == reviewer.Id && x.Title.Contains("DUE-1"));
        Assert.All(await db.OnboardingStepProgresses.ToListAsync(), step => Assert.NotNull(step.OverdueNotifiedAtUtc));
    }

    [Fact]
    public async Task Old_notifications_are_purged_by_read_state_and_age()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var person = await CreateEmployeeAsync(admin, token, "RET-1", "retention.person@integration.local");
        await using (var seed = _factory!.Services.CreateAsyncScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            var organizationId = await db.Employees.Where(x => x.Id == person.Id).Select(x => x.OrganizationId).SingleAsync();
            Notification Make(string title, bool read)
            {
                var notification = Notification.Create(organizationId, person.Id, NotificationType.QuizUnlocked, title, "Message", "/onboarding");
                if (read) notification.MarkRead(DateTimeOffset.UtcNow);
                return notification;
            }
            db.Notifications.AddRange(Make("old-read", true), Make("old-unread", false), Make("ancient-unread", false), Make("fresh-read", true));
            await db.SaveChangesAsync();
            var now = DateTimeOffset.UtcNow;
            async Task Backdate(string title, int days) => await db.Notifications.Where(x => x.Title == title)
                .ExecuteUpdateAsync(x => x.SetProperty(n => n.CreatedAtUtc, now.AddDays(-days)));
            await Backdate("old-read", NotificationRetention.ReadDays + 1);
            await Backdate("old-unread", NotificationRetention.ReadDays + 1);
            await Backdate("ancient-unread", NotificationRetention.UnreadDays + 1);
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
            Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<INotificationRetentionProcessor>().PurgeExpiredAsync(CancellationToken.None));

        await using var check = _factory.Services.CreateAsyncScope();
        var remaining = await check.ServiceProvider.GetRequiredService<AppDbContext>().Notifications
            .Where(x => x.RecipientEmployeeId == person.Id).Select(x => x.Title).OrderBy(x => x).ToListAsync();
        Assert.Equal(["fresh-read", "old-unread"], remaining);
    }

    [Fact]
    public async Task Listing_journeys_uses_a_constant_number_of_queries()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var package = await CreatePublishedPackageAsync(admin, token, "Company onboarding", "Company", null);
        var reviewer = await CreateEmployeeAsync(admin, token, "QRY-R", "query.reviewer@integration.local");
        async Task AssignAsync(int count, int offset)
        {
            for (var index = 0; index < count; index++)
            {
                var person = await CreateEmployeeAsync(admin, token, $"QRY-{offset + index}", $"query.{offset + index}@integration.local");
                await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/assignments", new
                {
                    employeeId = person.Id, templateId = package.Id, projectId = (Guid?)null, managerEmployeeId = reviewer.Id,
                    mentorEmployeeId = (Guid?)null, startDate = "2026-11-10"
                }, token);
            }
        }
        async Task<int> CountListQueriesAsync()
        {
            using var counter = new SqlCommandCounter("hr_api_tests");
            var list = await ReadJsonAsync(await admin.GetAsync("/api/v1/onboarding/assignments"));
            Assert.All(list.EnumerateArray(), x => Assert.Equal("Company onboarding", x.GetProperty("templateName").GetString()));
            return counter.Count;
        }

        await AssignAsync(3, 0);
        var small = await CountListQueriesAsync();
        await AssignAsync(6, 3);
        var large = await CountListQueriesAsync();

        Assert.Equal(small, large);
        Assert.True(large <= 15, $"Listing journeys sent {large} SQL commands.");
    }

    [Fact]
    public async Task Employee_reads_required_item_fails_rereads_and_passes_the_quiz()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var article = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/knowledge/articles", new
        {
            slug = "security-basics", title = "Security basics", description = "How we keep data safe", category = "Policies",
            content = "Lock your screen. Report incidents within one hour.", audience = "AllEmployees",
            projectId = (Guid?)null, departmentId = (Guid?)null, sortOrder = 0
        }, token));
        var articleId = article.GetProperty("id").GetGuid();
        await SendWithEtagAsync(admin, HttpMethod.Post, $"/api/v1/knowledge/articles/{articleId}/publish", new { }, token, article.GetProperty("version").GetInt64());

        var template = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/templates", new
        {
            name = "Company onboarding", description = "Company basics", durationDays = 30, kind = "Company",
            steps = new[]
            {
                new { sectionTitle = "Policies", sectionOrder = 0, title = "Security basics", instructions = "Read it",
                    expectedOutcome = "Know the rules", stepType = "ReadArticle", stepOrder = 0, dueAfterDays = 3, isRequired = true,
                    knowledgeArticleId = (Guid?)articleId }
            }
        }, token));
        var templateId = template.GetProperty("id").GetGuid();
        await SendWithEtagAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/templates/{templateId}/publish", new { }, token, template.GetProperty("version").GetInt64());

        var quiz = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/templates/{templateId}/quiz",
            new { title = "Company quiz", passingPercentage = 100, questionCount = 1 }, token));
        var quizId = quiz.GetProperty("id").GetGuid();
        var source = (await ReadJsonAsync(await admin.GetAsync($"/api/v1/onboarding/templates/{templateId}/quiz-sources")))[0];
        var withQuestion = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/quizzes/{quizId}/questions", new
        {
            questionText = "How quickly must incidents be reported?", explanation = "The policy says within one hour.", topic = "Incidents",
            difficulty = "Easy", sortOrder = 0, sourceDocumentId = (Guid?)null, sourceDocumentVersionId = (Guid?)null,
            sourceKnowledgeRevisionId = source.GetProperty("knowledgeRevisionId").GetGuid(), sourceLabel = source.GetProperty("label").GetString(),
            sourcePage = (int?)null, sourceParagraph = (int?)null,
            options = new[]
            {
                new { text = "Within one hour", isCorrect = true, sortOrder = 0 },
                new { text = "Within a week", isCorrect = false, sortOrder = 1 },
                new { text = "Never", isCorrect = false, sortOrder = 2 },
                new { text = "At the next review", isCorrect = false, sortOrder = 3 }
            }
        }, token));
        var options = withQuestion.GetProperty("questions")[0].GetProperty("options").EnumerateArray().ToArray();
        var correctId = options.Single(x => x.GetProperty("isCorrect").GetBoolean()).GetProperty("id").GetGuid();
        var wrongId = options.First(x => !x.GetProperty("isCorrect").GetBoolean()).GetProperty("id").GetGuid();
        await SendWithEtagAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/quizzes/{quizId}/publish", new { }, token, withQuestion.GetProperty("version").GetInt64());

        var learner = await CreateEmployeeAsync(admin, token, "LRN-1", "learner@integration.local");
        await CreateUserAsync(admin, token, "learner.user@integration.local", "Learner-Password-123!", "Employee", learner.Id);
        await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/assignments", new
        {
            employeeId = learner.Id, templateId, projectId = (Guid?)null, managerEmployeeId = (Guid?)null,
            mentorEmployeeId = (Guid?)null, startDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        }, token);

        using var employee = Client();
        var employeeToken = await LoginAsync(employee, "learner.user@integration.local", "Learner-Password-123!");
        async Task<JsonElement> JourneyAsync() => (await ReadJsonAsync(await employee.GetAsync("/api/v1/onboarding/me"))).GetProperty("journeys")[0];

        var journey = await JourneyAsync();
        var journeyId = journey.GetProperty("id").GetGuid();
        var assessment = journey.GetProperty("assessment");
        Assert.Equal("Locked", assessment.GetProperty("status").GetString());
        var item = Assert.Single(assessment.GetProperty("readingItems").EnumerateArray());
        Assert.Equal(1, assessment.GetProperty("requiredDocuments").GetInt32());

        var locked = await SendAsync(employee, HttpMethod.Post, $"/api/v1/onboarding/assignments/{journeyId}/quiz-attempts", new { }, employeeToken, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);

        async Task ReadEverythingAsync(JsonElement reading)
        {
            var url = $"/api/v1/onboarding/assignments/{journeyId}/reading/{reading.GetProperty("stepProgressId").GetGuid()}";
            var unopened = await SendWithEtagStatusAsync(employee, HttpMethod.Put, url, new { status = "Read" }, employeeToken, reading.GetProperty("version").GetInt64());
            Assert.Equal(HttpStatusCode.BadRequest, unopened);
            await SendWithEtagAsync(employee, HttpMethod.Put, url, new { status = "InProgress" }, employeeToken, reading.GetProperty("version").GetInt64());
            var opened = (await JourneyAsync()).GetProperty("assessment").GetProperty("readingItems")[0];
            await SendWithEtagAsync(employee, HttpMethod.Put, url, new { status = "Read" }, employeeToken, opened.GetProperty("version").GetInt64());
        }
        async Task<JsonElement> TakeQuizAsync(Guid answer)
        {
            var attempt = await ReadJsonAsync(await SendAsync(employee, HttpMethod.Post, $"/api/v1/onboarding/assignments/{journeyId}/quiz-attempts", new { }, employeeToken));
            var questionId = attempt.GetProperty("questions")[0].GetProperty("id").GetGuid();
            Assert.DoesNotContain("isCorrect", attempt.GetRawText(), StringComparison.OrdinalIgnoreCase);
            return await ReadJsonAsync(await SendWithEtagAsync(employee, HttpMethod.Post,
                $"/api/v1/onboarding/quiz-attempts/{attempt.GetProperty("id").GetGuid()}/submit",
                new { answers = new[] { new { questionId, selectedOptionId = answer } } }, employeeToken, attempt.GetProperty("version").GetInt64()));
        }

        await ReadEverythingAsync(item);
        Assert.Equal("Available", (await JourneyAsync()).GetProperty("assessment").GetProperty("status").GetString());

        var failed = await TakeQuizAsync(wrongId);
        Assert.Equal("Failed", failed.GetProperty("status").GetString());
        Assert.True(failed.GetProperty("readingReset").GetBoolean());
        Assert.Empty(failed.GetProperty("answerReview").EnumerateArray());
        Assert.Single(failed.GetProperty("documentScores").EnumerateArray());

        var reset = (await JourneyAsync()).GetProperty("assessment");
        Assert.Equal("Locked", reset.GetProperty("status").GetString());
        Assert.Equal(2, reset.GetProperty("readingCycleNumber").GetInt32());
        var unread = Assert.Single(reset.GetProperty("readingItems").EnumerateArray());
        Assert.Equal("Unread", unread.GetProperty("status").GetString());

        await ReadEverythingAsync(unread);
        var passed = await TakeQuizAsync(correctId);
        Assert.Equal("Passed", passed.GetProperty("status").GetString());
        Assert.Single(passed.GetProperty("answerReview").EnumerateArray());
        var finished = await JourneyAsync();
        Assert.Equal("Passed", finished.GetProperty("assessment").GetProperty("status").GetString());
        Assert.Equal("Completed", finished.GetProperty("status").GetString());
        Assert.Equal(2, finished.GetProperty("assessment").GetProperty("attempts").GetArrayLength());
    }

    [Fact]
    public async Task Hr_chooses_which_documents_an_employee_reads_and_the_quiz_follows()
    {
        using var admin = Client();
        var token = await LoginAsync(admin, AdminEmail, AdminPassword);
        var security = await CreatePublishedArticleAsync(admin, token, "security-rules", "Security rules", "Report incidents within one hour.");
        var quality = await CreatePublishedArticleAsync(admin, token, "quality-rules", "Quality rules", "Every deviation needs a CAPA record.");
        var template = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/templates", new
        {
            name = "Company onboarding", description = "Company basics", durationDays = 30, kind = "Company",
            steps = new[]
            {
                new { sectionTitle = "Policies", sectionOrder = 0, title = "Security rules", instructions = "Read it", expectedOutcome = "Know it",
                    stepType = "ReadArticle", stepOrder = 0, dueAfterDays = 3, isRequired = true, knowledgeArticleId = (Guid?)security },
                new { sectionTitle = "Policies", sectionOrder = 1, title = "Quality rules", instructions = "Read it", expectedOutcome = "Know it",
                    stepType = "ReadArticle", stepOrder = 1, dueAfterDays = 3, isRequired = true, knowledgeArticleId = (Guid?)quality }
            }
        }, token));
        var templateId = template.GetProperty("id").GetGuid();
        await SendWithEtagAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/templates/{templateId}/publish", new { }, token, template.GetProperty("version").GetInt64());
        var quiz = await ReadJsonAsync(await SendAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/templates/{templateId}/quiz",
            new { title = "Company quiz", passingPercentage = 50, questionCount = 2 }, token));
        var quizId = quiz.GetProperty("id").GetGuid();
        var sources = (await ReadJsonAsync(await admin.GetAsync($"/api/v1/onboarding/templates/{templateId}/quiz-sources"))).EnumerateArray().ToArray();
        await AddQuestionAsync(admin, token, quizId, sources.Single(x => x.GetProperty("label").GetString()!.Contains("Security")), "How fast are incidents reported?");
        var withQuestions = await AddQuestionAsync(admin, token, quizId, sources.Single(x => x.GetProperty("label").GetString()!.Contains("Quality")), "What does a deviation need?");
        await SendWithEtagAsync(admin, HttpMethod.Post, $"/api/v1/onboarding/quizzes/{quizId}/publish", new { }, token, withQuestions.GetProperty("version").GetInt64());

        var options = await ReadJsonAsync(await admin.GetAsync($"/api/v1/onboarding/templates/{templateId}/assignment-options"));
        var steps = options.GetProperty("packages")[0].GetProperty("readingSteps").EnumerateArray().ToArray();
        Assert.Equal(2, steps.Length);
        Assert.All(steps, step => Assert.Equal(1, step.GetProperty("questionCount").GetInt32()));
        var securityStepId = steps.Single(x => x.GetProperty("title").GetString() == "Security rules").GetProperty("templateStepId").GetGuid();

        var learner = await CreateEmployeeAsync(admin, token, "PICK-1", "picked@integration.local");
        await CreateUserAsync(admin, token, "picked.user@integration.local", "Picked-Password-123!", "Employee", learner.Id);
        var invalid = await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/assignments", new
        {
            employeeId = learner.Id, templateId, startDate = "2026-11-10", readingStepIds = new[] { Guid.NewGuid() }
        }, token, ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        await SendAsync(admin, HttpMethod.Post, "/api/v1/onboarding/assignments", new
        {
            employeeId = learner.Id, templateId, startDate = "2026-11-10", readingStepIds = new[] { securityStepId }
        }, token);

        using var employee = Client();
        var employeeToken = await LoginAsync(employee, "picked.user@integration.local", "Picked-Password-123!");
        var journey = (await ReadJsonAsync(await employee.GetAsync("/api/v1/onboarding/me"))).GetProperty("journeys")[0];
        var journeyId = journey.GetProperty("id").GetGuid();
        var item = Assert.Single(journey.GetProperty("assessment").GetProperty("readingItems").EnumerateArray());
        Assert.Equal("Security rules", item.GetProperty("title").GetString());
        Assert.DoesNotContain(journey.GetProperty("steps").EnumerateArray(), x => x.GetProperty("title").GetString() == "Quality rules");

        var url = $"/api/v1/onboarding/assignments/{journeyId}/reading/{item.GetProperty("stepProgressId").GetGuid()}";
        await SendWithEtagAsync(employee, HttpMethod.Put, url, new { status = "InProgress" }, employeeToken, item.GetProperty("version").GetInt64());
        var opened = (await ReadJsonAsync(await employee.GetAsync("/api/v1/onboarding/me"))).GetProperty("journeys")[0]
            .GetProperty("assessment").GetProperty("readingItems")[0];
        await SendWithEtagAsync(employee, HttpMethod.Put, url, new { status = "Read" }, employeeToken, opened.GetProperty("version").GetInt64());

        var attempt = await ReadJsonAsync(await SendAsync(employee, HttpMethod.Post, $"/api/v1/onboarding/assignments/{journeyId}/quiz-attempts", new { }, employeeToken));
        var question = Assert.Single(attempt.GetProperty("questions").EnumerateArray());
        Assert.Equal("How fast are incidents reported?", question.GetProperty("questionText").GetString());
    }

    private static async Task<Guid> CreatePublishedArticleAsync(HttpClient client, string token, string slug, string title, string content)
    {
        var article = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/knowledge/articles", new
        {
            slug, title, description = title, category = "Policies", content, audience = "AllEmployees",
            projectId = (Guid?)null, departmentId = (Guid?)null, sortOrder = 0
        }, token));
        var id = article.GetProperty("id").GetGuid();
        await SendWithEtagAsync(client, HttpMethod.Post, $"/api/v1/knowledge/articles/{id}/publish", new { }, token, article.GetProperty("version").GetInt64());
        return id;
    }

    private static async Task<JsonElement> AddQuestionAsync(HttpClient client, string token, Guid quizId, JsonElement source, string text) =>
        await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, $"/api/v1/onboarding/quizzes/{quizId}/questions", new
        {
            questionText = text, explanation = "From the article.", topic = "Policy", difficulty = "Easy", sortOrder = 0,
            sourceDocumentId = (Guid?)null, sourceDocumentVersionId = (Guid?)null,
            sourceKnowledgeRevisionId = source.GetProperty("knowledgeRevisionId").GetGuid(), sourceLabel = source.GetProperty("label").GetString(),
            sourcePage = (int?)null, sourceParagraph = (int?)null,
            options = new[]
            {
                new { text = "Correct", isCorrect = true, sortOrder = 0 }, new { text = "Wrong A", isCorrect = false, sortOrder = 1 },
                new { text = "Wrong B", isCorrect = false, sortOrder = 2 }, new { text = "Wrong C", isCorrect = false, sortOrder = 3 }
            }
        }, token));

    private static async Task<HttpStatusCode> SendWithEtagStatusAsync(HttpClient client, HttpMethod method, string url, object body, string token, long version)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return (await client.SendAsync(request)).StatusCode;
    }

    /// <summary>Counts SQL commands EF Core executes against one database while it is alive.</summary>
    private sealed class SqlCommandCounter : IObserver<System.Diagnostics.DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly string _database;
        private readonly List<IDisposable> _subscriptions = [];
        private int _count;

        public SqlCommandCounter(string database)
        {
            _database = database;
            _subscriptions.Add(System.Diagnostics.DiagnosticListener.AllListeners.Subscribe(this));
        }

        public int Count => Volatile.Read(ref _count);

        public void OnNext(System.Diagnostics.DiagnosticListener listener)
        {
            if (listener.Name == DbLoggerCategory.Name) _subscriptions.Add(listener.Subscribe(this));
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == RelationalEventId.CommandExecuted.Name &&
                value.Value is Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData data &&
                data.Command.Connection?.Database == _database)
                Interlocked.Increment(ref _count);
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void Dispose() => _subscriptions.ForEach(x => x.Dispose());
    }

    private static async Task<JsonElement[]> ActiveJourneysAsync(HttpClient client, Guid employeeId)
    {
        var assignments = await ReadJsonAsync(await client.GetAsync($"/api/v1/onboarding/assignments?employeeId={employeeId}"));
        return assignments.EnumerateArray().Where(x => x.GetProperty("status").GetString() != "Superseded").ToArray();
    }

    private static async Task<(Guid Id, long Version)> CreatePublishedPackageAsync(HttpClient client, string token, string name, string kind, Guid? projectId)
    {
        var created = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/onboarding/templates", new
        {
            name, description = $"{name} package", durationDays = 30, kind, projectId,
            steps = new[]
            {
                new { sectionTitle = name, sectionOrder = 0, title = $"{name} kickoff", instructions = "Attend the kickoff",
                    expectedOutcome = "Know the basics", stepType = "PracticalTask", stepOrder = 0, dueAfterDays = 1, isRequired = true }
            }
        }, token));
        var id = created.GetProperty("id").GetGuid();
        var published = await ReadJsonAsync(await SendWithEtagAsync(client, HttpMethod.Post, $"/api/v1/onboarding/templates/{id}/publish",
            new { }, token, created.GetProperty("version").GetInt64()));
        return (id, published.GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task Background_job_identity_carries_only_organization_and_requesting_user()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        scope.ServiceProvider.GetRequiredService<HrManagement.Infrastructure.Identity.BackgroundJobContext>()
            .RunAs(organizationId, userId, "quiz-generation:test");

        var currentUser = scope.ServiceProvider.GetRequiredService<HrManagement.Application.Common.Interfaces.ICurrentUserService>();

        Assert.Equal(organizationId, currentUser.OrganizationId);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Null(currentUser.EmployeeId);
        Assert.False(currentUser.IsInRole("HR"));
        Assert.Equal("quiz-generation:test", currentUser.CorrelationId);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, rememberMe = false });
        login.EnsureSuccessStatusCode();
        var json = await client.GetStringAsync("/api/v1/auth/antiforgery");
        return JsonDocument.Parse(json).RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<(Guid Id, long Version)> CreateEmployeeAsync(HttpClient client, string token, string number, string email)
    {
        var response = await SendAsync(client, HttpMethod.Post, "/api/v1/employees", new
        { employeeNumber = number, firstName = number, lastName = "Person", workEmail = email, phone = (string?)null }, token);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return (json.GetProperty("id").GetGuid(), json.GetProperty("version").GetInt64());
    }

    private static async Task<Guid> CreateUserAsync(HttpClient client, string token, string email, string password, string role, Guid employeeId)
    {
        var response = await SendAsync(client, HttpMethod.Post, "/api/v1/users", new { email, temporaryPassword = password, role, employeeId }, token);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateNamedEmployeeAsync(HttpClient client, string token, string number, string firstName, string lastName)
    {
        var response = await SendAsync(client, HttpMethod.Post, "/api/v1/employees", new
        {
            employeeNumber = number,
            firstName,
            lastName,
            workEmail = $"{firstName}.{lastName}.{Guid.NewGuid():N}@integration.local",
            phone = (string?)null,
            dateOfBirth = (string?)null
        }, token);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateAiConversationAsync(HttpClient client, string token, string title)
    {
        var created = await ReadJsonAsync(await SendAsync(client, HttpMethod.Post, "/api/v1/ai/conversations",
            new { title, contextType = (string?)null, contextId = (Guid?)null }, token));
        return created.GetProperty("id").GetGuid();
    }

    private async Task SeedIndexedDocumentAsync(Guid employeeId, string documentType, string displayName, string text, int page)
    {
        using var serviceScope = _factory!.Services.CreateScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await db.Employees.IgnoreQueryFilters().SingleAsync(x => x.Id == employeeId);
        var approverId = await db.Users.Where(x => x.OrganizationId == employee.OrganizationId && x.NormalizedEmail == AdminEmail.ToUpperInvariant())
            .Select(x => x.Id).SingleAsync();
        var now = DateTimeOffset.UtcNow;
        var document = EmployeeDocument.Create(employee.OrganizationId, displayName, documentType);
        document.MarkClean(now);
        document.ApproveAiIndexing(DataClassification.Private, approverId, now);
        var version = DocumentVersion.Create(employee.OrganizationId, document.Id, 1, $"{displayName}.pdf",
            $"documents/{employee.OrganizationId}/{document.Id}/original.pdf", "application/pdf", text.Length);
        version.SetScanResult(new string('a', 64), null, now);
        var association = DocumentAssociation.Create(employee.OrganizationId, document.Id, DocumentAssociationType.Employee, employeeId);
        var index = DocumentAiIndex.Create(employee.OrganizationId, document.Id, version.Id, true);
        index.Complete("fake-embedding", now);
        var chunk = DocumentChunk.Create(employee.OrganizationId, document.Id, version.Id, 0, text, text.Split(' ').Length,
            page, null, FakeEmbeddingProvider.Vector());
        db.Documents.Add(document);
        db.DocumentVersions.Add(version);
        db.DocumentAssociations.Add(association);
        db.DocumentAiIndexes.Add(index);
        db.DocumentChunks.Add(chunk);
        await db.SaveChangesAsync();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, object body, string token, bool ensureSuccess = true)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await client.SendAsync(request);
        if (ensureSuccess) response.EnsureSuccessStatusCode();
        return response;
    }

    private static async Task<HttpResponseMessage> SendWithEtagAsync(HttpClient client, HttpMethod method, string url, object body, string token, long version)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private sealed class FakeAiProvider : IAiProvider
    {
        public Task<AiProviderResponse> CompleteAsync(AiProviderRequest request, CancellationToken cancellationToken)
        {
            if (!request.SystemPrompt.Contains("trusted current business date", StringComparison.Ordinal) ||
                !request.SystemPrompt.Contains("untrusted evidence", StringComparison.Ordinal))
                throw new InvalidOperationException("Trusted date and prompt-injection protections were not supplied.");
            if (request.Exchanges.Count > 0 &&
                (request.ProviderTurns is null || request.ProviderTurns.Sum(x => x.ToolExchanges.Count) != request.Exchanges.Count))
                throw new InvalidOperationException("Provider tool turns were not preserved in order.");
            var prompt = request.Messages.LastOrDefault(x => x.Role == "user")?.Content ?? string.Empty;
            if (prompt.Contains("favorite food", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new AiProviderResponse("The HR system does not contain available evidence for that information.", [], [], 12, 6, "fake-ai"));
            if (prompt.Contains("information about Erion", StringComparison.OrdinalIgnoreCase))
                return GeneralProfileResponse(request);
            if (prompt.Contains("contracts expire this month", StringComparison.OrdinalIgnoreCase))
                return ContractExpiryResponse(request);
            if (prompt.Contains("notice period", StringComparison.OrdinalIgnoreCase))
                return NoticePeriodResponse(request);
            if (prompt.Contains("Which employees have React Native", StringComparison.OrdinalIgnoreCase))
                return CvExperienceResponse(request);
            if (prompt.Contains("Software Engineers whose contracts expire this year", StringComparison.OrdinalIgnoreCase))
                return HybridResponse(request);
            if (!prompt.Contains("birthday", StringComparison.OrdinalIgnoreCase) &&
                !prompt.Contains("manager", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new AiProviderResponse("No authorized evidence was requested.", [], [], 12, 6, "fake-ai"));

            if (request.Exchanges.Count == 0)
            {
                var search = prompt.Contains("John", StringComparison.OrdinalIgnoreCase) ? "John" : "Erion";
                var arguments = JsonSerializer.Serialize(new
                {
                    search,
                    position = (string?)null,
                    organizationUnit = (string?)null,
                    employmentStatus = (string?)null,
                    organizationUnitId = (Guid?)null,
                    managerEmployeeId = (Guid?)null,
                    hiredFrom = (DateOnly?)null,
                    hiredBefore = (DateOnly?)null,
                    birthMonth = (int?)null,
                    birthDay = (int?)null,
                    limit = 10
                });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("search-1", "search_employees", arguments)], 12, 2, "fake-ai"));
            }

            var searchOutput = JsonDocument.Parse(request.Exchanges[0].OutputJson).RootElement;
            if (searchOutput.GetProperty("data").GetArrayLength() > 1)
            {
                var sources = searchOutput.GetProperty("sources").EnumerateArray().Select(x => Property(x, "id").GetString()!).ToArray();
                return Task.FromResult(new AiProviderResponse("Multiple employees match John. Please clarify which employee you mean.", sources, [], 12, 6, "fake-ai"));
            }
            var employeeId = searchOutput.GetProperty("data")[0].GetProperty("id").GetGuid();
            if (request.Exchanges.Count == 1)
            {
                var fields = prompt.Contains("manager", StringComparison.OrdinalIgnoreCase) ? new[] { "manager" } : new[] { "dateOfBirth" };
                var arguments = JsonSerializer.Serialize(new { employeeId, fields });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("profile-1", "get_employee_profile", arguments)], 10, 2, "fake-ai"));
            }

            var profileOutput = JsonDocument.Parse(request.Exchanges[1].OutputJson).RootElement;
            var sourceId = $"employee:{employeeId}";
            if (prompt.Contains("manager", StringComparison.OrdinalIgnoreCase))
            {
                var manager = profileOutput.GetProperty("data").GetProperty("manager").GetProperty("name").GetString();
                return Task.FromResult(new AiProviderResponse($"Erion's manager is {manager}.", [sourceId], [], 16, 8, "fake-ai"));
            }
            var birthDate = profileOutput.GetProperty("data").GetProperty("dateOfBirth").GetString();
            return Task.FromResult(new AiProviderResponse($"Erion's birthday is {birthDate}.", [sourceId], [], 16, 8, "fake-ai"));
        }

        private static Task<AiProviderResponse> ContractExpiryResponse(AiProviderRequest request)
        {
            if (request.Exchanges.Count == 0)
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");
                var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
                var first = new DateOnly(today.Year, today.Month, 1);
                var arguments = JsonSerializer.Serialize(new
                {
                    employeeId = (Guid?)null,
                    employeeIds = (Guid[]?)null,
                    status = "Active",
                    startsFrom = (DateOnly?)null,
                    startsBefore = (DateOnly?)null,
                    endsFrom = first,
                    endsBefore = first.AddMonths(1),
                    organizationUnitId = (Guid?)null,
                    limit = 100
                });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("contract-1", "search_contracts", arguments)], 12, 2, "fake-ai"));
            }
            var output = JsonDocument.Parse(request.Exchanges[0].OutputJson).RootElement;
            var sources = output.GetProperty("sources").EnumerateArray().Select(x => Property(x, "id").GetString()!).ToArray();
            return Task.FromResult(new AiProviderResponse($"I found {sources.Length} active contract(s) expiring this month.", sources, [], 16, 8, "fake-ai"));
        }

        private static Task<AiProviderResponse> NoticePeriodResponse(AiProviderRequest request)
        {
            if (request.Exchanges.Count == 0)
                return Task.FromResult(SearchEmployee("Erion"));
            if (request.Exchanges.Count == 1)
            {
                var employeeId = Data(request.Exchanges[0])[0].GetProperty("id").GetGuid();
                var arguments = JsonSerializer.Serialize(new { employeeId, employeeIds = (Guid[]?)null, search = "notice period", documentType = "Employment Contract", limit = 8 });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("document-1", "search_employee_documents", arguments)], 12, 2, "fake-ai"));
            }
            var source = Sources(request.Exchanges[1]).Single();
            return Task.FromResult(new AiProviderResponse("Erion's employment contract requires 30 calendar days notice.", [source], [], 16, 8, "fake-ai"));
        }

        private static Task<AiProviderResponse> GeneralProfileResponse(AiProviderRequest request)
        {
            if (request.Exchanges.Count == 0)
                return Task.FromResult(SearchEmployee("Erion"));
            var employeeId = Data(request.Exchanges[0])[0].GetProperty("id").GetGuid();
            if (request.Exchanges.Count == 1)
            {
                var arguments = JsonSerializer.Serialize(new
                {
                    employeeId,
                    fields = new[] { "employeeNumber", "workEmail", "employmentStatus", "hireDate", "position", "organizationUnit", "manager" }
                });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("profile-1", "get_employee_profile", arguments)], 10, 2, "fake-ai"));
            }
            var profile = Data(request.Exchanges[1]);
            var name = $"{profile.GetProperty("firstName").GetString()} {profile.GetProperty("lastName").GetString()}";
            var email = profile.GetProperty("workEmail").GetString();
            return Task.FromResult(new AiProviderResponse($"{name}'s work email is {email}.", [$"employee:{employeeId}"], [], 16, 8, "fake-ai"));
        }

        private static Task<AiProviderResponse> CvExperienceResponse(AiProviderRequest request)
        {
            if (request.Exchanges.Count == 0)
            {
                var arguments = JsonSerializer.Serialize(new { employeeId = (Guid?)null, employeeIds = (Guid[]?)null, search = "React Native experience", documentType = "CV", limit = 8 });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("document-1", "search_employee_documents", arguments)], 12, 2, "fake-ai"));
            }
            var documentData = Data(request.Exchanges[0]);
            if (request.Exchanges.Count == 1)
            {
                var employeeId = documentData[0].GetProperty("employeeId").GetGuid();
                var arguments = JsonSerializer.Serialize(new { employeeId, fields = new[] { "employeeNumber" } });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("profile-1", "get_employee_profile", arguments)], 10, 2, "fake-ai"));
            }
            var profile = Data(request.Exchanges[1]);
            var name = $"{profile.GetProperty("firstName").GetString()} {profile.GetProperty("lastName").GetString()}";
            var sources = Sources(request.Exchanges[0]).Concat(Sources(request.Exchanges[1])).ToArray();
            return Task.FromResult(new AiProviderResponse($"{name} has React Native experience according to the approved CV.", sources, [], 16, 8, "fake-ai"));
        }

        private static Task<AiProviderResponse> HybridResponse(AiProviderRequest request)
        {
            var year = DateTimeOffset.UtcNow.Year;
            if (request.Exchanges.Count == 0)
            {
                var arguments = JsonSerializer.Serialize(new
                {
                    search = (string?)null,
                    position = "Software Engineer",
                    organizationUnit = (string?)null,
                    employmentStatus = (string?)null,
                    organizationUnitId = (Guid?)null,
                    managerEmployeeId = (Guid?)null,
                    hiredFrom = (DateOnly?)null,
                    hiredBefore = (DateOnly?)null,
                    birthMonth = (int?)null,
                    birthDay = (int?)null,
                    limit = 100
                });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("employee-1", "search_employees", arguments)], 12, 2, "fake-ai"));
            }
            var employeeIds = Data(request.Exchanges[0]).EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
            if (request.Exchanges.Count == 1)
            {
                var arguments = JsonSerializer.Serialize(new
                {
                    employeeId = (Guid?)null,
                    employeeIds,
                    status = "Active",
                    startsFrom = (DateOnly?)null,
                    startsBefore = (DateOnly?)null,
                    endsFrom = new DateOnly(year, 1, 1),
                    endsBefore = new DateOnly(year + 1, 1, 1),
                    organizationUnitId = (Guid?)null,
                    limit = 100
                });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("contract-1", "search_contracts", arguments)], 12, 2, "fake-ai"));
            }
            var contractEmployeeIds = Data(request.Exchanges[1]).EnumerateArray().Select(x => x.GetProperty("employeeId").GetGuid()).ToArray();
            if (request.Exchanges.Count == 2)
            {
                var arguments = JsonSerializer.Serialize(new { employeeId = (Guid?)null, employeeIds = contractEmployeeIds, search = "React Native", documentType = "CV", limit = 8 });
                return Task.FromResult(new AiProviderResponse(string.Empty, [], [new AiToolCall("document-1", "search_employee_documents", arguments)], 12, 2, "fake-ai"));
            }
            var employee = Data(request.Exchanges[0])[0];
            var name = $"{employee.GetProperty("firstName").GetString()} {employee.GetProperty("lastName").GetString()}";
            var sources = request.Exchanges.SelectMany(Sources).ToArray();
            return Task.FromResult(new AiProviderResponse($"{name} is a Software Engineer with a contract expiring this year and React Native experience in the approved CV.", sources, [], 20, 10, "fake-ai"));
        }

        private static AiProviderResponse SearchEmployee(string search)
        {
            var arguments = JsonSerializer.Serialize(new
            {
                search,
                position = (string?)null,
                organizationUnit = (string?)null,
                employmentStatus = (string?)null,
                organizationUnitId = (Guid?)null,
                managerEmployeeId = (Guid?)null,
                hiredFrom = (DateOnly?)null,
                hiredBefore = (DateOnly?)null,
                birthMonth = (int?)null,
                birthDay = (int?)null,
                limit = 10
            });
            return new AiProviderResponse(string.Empty, [], [new AiToolCall("search-1", "search_employees", arguments)], 12, 2, "fake-ai");
        }

        private static JsonElement Data(AiToolExchange exchange) => JsonDocument.Parse(exchange.OutputJson).RootElement.GetProperty("data").Clone();
        private static string[] Sources(AiToolExchange exchange) => JsonDocument.Parse(exchange.OutputJson).RootElement.GetProperty("sources")
            .EnumerateArray().Select(x => Property(x, "id").GetString()!).ToArray();

        private static JsonElement Property(JsonElement value, string name) => value.EnumerateObject()
            .Single(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Value;
        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeAiUsageService : IAiUsageService
    {
        public Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken) => Task.FromResult<IAsyncDisposable>(new Lease());
        public Task RecordAsync(Guid messageId, string model, int inputTokens, int outputTokens, CancellationToken cancellationToken) => Task.CompletedTask;
        private sealed class Lease : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
    }

    private sealed class FakeEmbeddingProvider : IEmbeddingProvider
    {
        public Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<float[]>>(inputs.Select(_ => Vector()).ToArray());

        public static float[] Vector()
        {
            var vector = new float[1536];
            vector[0] = 1;
            return vector;
        }
    }
}
