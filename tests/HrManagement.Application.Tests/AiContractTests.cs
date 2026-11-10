using HrManagement.Application.Features.Ai;
using HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Domain;
using System.Text.Json;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class AiContractTests
{
    [Fact]
    public void Ai_defaults_are_disabled_and_bounded()
    {
        var options = new AiOptions();

        Assert.False(options.Enabled);
        Assert.False(options.RealDataApproved);
        Assert.Equal(4, options.MaxToolRounds);
        Assert.Equal(6, options.MaxToolCallsPerTurn);
        Assert.Equal(1536, options.EmbeddingDimensions);
    }

    [Theory]
    [InlineData(DataClassification.Sensitive)]
    [InlineData(DataClassification.HighlyRestricted)]
    public void Document_ai_approval_validator_rejects_restricted_classes(DataClassification classification)
    {
        var validator = new ApproveDocumentForAiCommandValidator();
        var result = validator.Validate(new ApproveDocumentForAiCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), classification));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ApproveDocumentForAiCommand.Classification));
    }

    [Fact]
    public void Tool_schemas_are_general_purpose_strict_and_do_not_accept_authorization_values()
    {
        var registry = new AiToolRegistry(new StubDataReader(), new StubSettings());

        Assert.Equal(16, registry.Definitions.Count);
        var employees = registry.Definitions.Single(x => x.Name == "search_employees").JsonSchema;
        Assert.Contains("position", employees, StringComparison.Ordinal);
        Assert.Contains("hiredBefore", employees, StringComparison.Ordinal);
        Assert.Contains("birthMonth", employees, StringComparison.Ordinal);
        Assert.DoesNotContain("organizationId", employees, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sql", employees, StringComparison.OrdinalIgnoreCase);

        var profile = registry.Definitions.Single(x => x.Name == "get_employee_profile").JsonSchema;
        Assert.Contains("dateOfBirth", profile, StringComparison.Ordinal);
        Assert.Contains("fields", profile, StringComparison.Ordinal);

        var contracts = registry.Definitions.Single(x => x.Name == "search_contracts").JsonSchema;
        Assert.Contains("employeeIds", contracts, StringComparison.Ordinal);
        Assert.Contains("endsBefore", contracts, StringComparison.Ordinal);

        var documents = registry.Definitions.Single(x => x.Name == "search_employee_documents").JsonSchema;
        Assert.Contains("documentType", documents, StringComparison.Ordinal);
        Assert.Contains("employeeIds", documents, StringComparison.Ordinal);

        var projectKnowledge = registry.Definitions.Single(x => x.Name == "search_project_knowledge").JsonSchema;
        Assert.Contains("projectId", projectKnowledge, StringComparison.Ordinal);
        Assert.DoesNotContain("organizationId", projectKnowledge, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_tools_and_invalid_half_open_ranges_fail_before_data_access()
    {
        var registry = new AiToolRegistry(new StubDataReader(), new StubSettings());
        await Assert.ThrowsAsync<ValidationException>(() => registry.ExecuteAsync("run_sql", "{}", CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() => registry.ExecuteAsync("search_contracts", """
            {"employeeId":null,"employeeIds":null,"status":"Active","startsFrom":null,"startsBefore":null,"endsFrom":"2026-11-01","endsBefore":"2026-10-01","organizationUnitId":null,"limit":50}
            """, CancellationToken.None));
    }

    private sealed class StubSettings : IAiRuntimeSettings
    {
        public AiOptions Value { get; } = new();
        public bool ProviderConfigured => true;
        public bool UsagePricingConfigured => true;
    }

    private sealed class StubDataReader : IAiDataReader
    {
        private static Task<AiToolResult> Empty(AiResultType type) => Task.FromResult(new AiToolResult(type, JsonSerializer.SerializeToElement(Array.Empty<object>()), [], 0));
        public Task<AiToolResult> SearchEmployeesAsync(AiEmployeeSearch search, CancellationToken cancellationToken) => Empty(AiResultType.EmployeeList);
        public Task<AiToolResult> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken) => Empty(AiResultType.EmployeeDetails);
        public Task<AiToolResult> GetEmployeeHistoryAsync(Guid employeeId, CancellationToken cancellationToken) => Empty(AiResultType.EmployeeDetails);
        public Task<AiToolResult> GetDirectReportsAsync(Guid managerEmployeeId, CancellationToken cancellationToken) => Empty(AiResultType.EmployeeList);
        public Task<AiToolResult> GetOrganizationStructureAsync(int limit, CancellationToken cancellationToken) => Empty(AiResultType.OrganizationTree);
        public Task<AiToolResult> SearchContractsAsync(AiContractSearch search, CancellationToken cancellationToken) => Empty(AiResultType.ContractList);
        public Task<AiToolResult> GetEmployeeContractsAsync(Guid employeeId, CancellationToken cancellationToken) => Empty(AiResultType.ContractList);
        public Task<AiToolResult> ListEmployeeDocumentsAsync(Guid employeeId, CancellationToken cancellationToken) => Empty(AiResultType.DocumentList);
        public Task<AiToolResult> SearchEmployeeDocumentsAsync(AiDocumentSearch search, CancellationToken cancellationToken) => Empty(AiResultType.DocumentList);
        public Task<AiToolResult> GetMyOnboardingAsync(bool progressOnly, CancellationToken cancellationToken) => Empty(AiResultType.Onboarding);
        public Task<AiToolResult> GetAssignedProjectsAsync(CancellationToken cancellationToken) => Empty(AiResultType.ProjectList);
        public Task<AiToolResult> GetProjectOverviewAsync(Guid projectId, CancellationToken cancellationToken) => Empty(AiResultType.ProjectList);
        public Task<AiToolResult> GetProjectTeamAsync(Guid projectId, CancellationToken cancellationToken) => Empty(AiResultType.ProjectList);
        public Task<AiToolResult> SearchKnowledgeAsync(AiKnowledgeSearch search, CancellationToken cancellationToken) => Empty(AiResultType.KnowledgeList);
    }
}
