using HrManagement.Domain;
using Xunit;

namespace HrManagement.Domain.Tests;

public sealed class DomainInvariantTests
{
    [Fact]
    public void Employee_rejects_a_future_birth_date()
    {
        Assert.Throws<DomainException>(() => Employee.Create(Guid.CreateVersion7(), "E-1", "Future", "Person", "future@example.test", null,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)));
    }
    private static readonly Guid OrganizationId = Guid.CreateVersion7();

    [Fact]
    public void Employee_normalizes_identifiers_and_email()
    {
        var employee = Employee.Create(OrganizationId, " emp-7 ", " Ada ", " Lovelace ", " ADA@EXAMPLE.COM ", null);
        Assert.Equal("EMP-7", employee.EmployeeNumber);
        Assert.Equal("ada@example.com", employee.WorkEmail);
        Assert.Equal("Ada Lovelace", employee.FullName);
    }

    [Fact]
    public void Employment_rejects_an_end_before_start()
    {
        var error = Assert.Throws<DomainException>(() => Employment.Create(OrganizationId, Guid.NewGuid(), EmploymentStatus.Active, new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 31)));
        Assert.Contains("end date", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reporting_line_rejects_self_management()
    {
        var employeeId = Guid.CreateVersion7();
        Assert.Throws<DomainException>(() => ReportingLine.Create(OrganizationId, employeeId, employeeId, new DateOnly(2026, 1, 1), null));
    }

    [Fact]
    public void Contract_revision_advances_version_and_revision()
    {
        var contract = Contract.Create(OrganizationId, Guid.NewGuid(), "c-1", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        contract.AddRevision(new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31), DateTimeOffset.UtcNow);
        Assert.Equal(2, contract.CurrentRevision);
        Assert.Equal(2, contract.Version);
    }

    [Fact]
    public void Compensation_rejects_negative_amount()
    {
        Assert.Throws<DomainException>(() => CompensationRecord.Create(OrganizationId, Guid.NewGuid(), -1, "EUR", "Monthly", new DateOnly(2026, 1, 1), null));
    }

    [Fact]
    public void Document_replacement_creates_a_new_logical_version()
    {
        var document = EmployeeDocument.Create(OrganizationId, "contract.pdf", "Contract");
        document.MarkClean(DateTimeOffset.UtcNow);
        document.AddVersion(DateTimeOffset.UtcNow);
        Assert.Equal(2, document.CurrentVersion);
        Assert.Equal(DocumentStatus.PendingScan, document.Status);
    }

    [Fact]
    public void Employee_document_can_be_assigned_to_a_folder()
    {
        var folderId = Guid.CreateVersion7();
        var document = EmployeeDocument.Create(OrganizationId, "contract.pdf", "Contract", folderId);

        Assert.Equal(folderId, document.FolderId);
    }

    [Fact]
    public void Employee_document_folder_requires_an_employee_for_employee_scope()
    {
        Assert.Throws<DomainException>(() => DocumentFolder.Create(
            OrganizationId,
            "Personal documents",
            null,
            DocumentFolderScope.Employee,
            null,
            ["Employee"]));
    }

    [Fact]
    public void Ai_indexing_rejects_sensitive_documents()
    {
        var document = EmployeeDocument.Create(OrganizationId, "contract.pdf", "Contract");
        document.MarkClean(DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => document.ApproveAiIndexing(DataClassification.Sensitive, Guid.CreateVersion7(), DateTimeOffset.UtcNow));
        Assert.False(document.AiIndexingApproved);
    }

    [Fact]
    public void Replacing_a_document_revokes_prior_ai_approval()
    {
        var document = EmployeeDocument.Create(OrganizationId, "cv.pdf", "CV");
        document.MarkClean(DateTimeOffset.UtcNow);
        document.ApproveAiIndexing(DataClassification.Private, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        document.AddVersion(DateTimeOffset.UtcNow);

        Assert.False(document.AiIndexingApproved);
        Assert.Null(document.AiIndexingApprovedBy);
        Assert.Equal(DataClassification.Private, document.DataClassification);
    }

    [Fact]
    public void Document_ai_index_has_bounded_retry_lifecycle()
    {
        var index = DocumentAiIndex.Create(OrganizationId, Guid.CreateVersion7(), Guid.CreateVersion7(), true);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.True(index.TryLease(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1)));
            index.Fail("provider_unavailable", retry: true, DateTimeOffset.UtcNow);
        }

        Assert.Equal(DocumentAiIndexStatus.Failed, index.Status);
        Assert.Equal(5, index.Attempts);
    }

    [Fact]
    public void Organization_units_enforce_a_broad_to_specific_hierarchy()
    {
        Assert.True(Department.CanContain(OrganizationUnitType.BusinessUnit, OrganizationUnitType.Division));
        Assert.True(Department.CanContain(OrganizationUnitType.Division, OrganizationUnitType.Department));
        Assert.True(Department.CanContain(OrganizationUnitType.Department, OrganizationUnitType.Team));
        Assert.False(Department.CanContain(OrganizationUnitType.Team, OrganizationUnitType.Department));
        Assert.False(Department.CanContain(OrganizationUnitType.Department, OrganizationUnitType.Department));
    }

    [Fact]
    public void Organization_unit_rejects_itself_as_parent()
    {
        var department = Department.Create(OrganizationId, "Engineering", "ENG");

        Assert.Throws<DomainException>(() => department.Update(
            department.Name,
            department.Code,
            OrganizationUnitType.Department,
            department.Id,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Department_leadership_responsibilities_require_distinct_employees()
    {
        var employeeId = Guid.CreateVersion7();

        Assert.Throws<DomainException>(() => Department.Create(
            OrganizationId,
            "Technology",
            "TECH",
            managerEmployeeId: employeeId,
            hrEmployeeId: employeeId,
            productManagerEmployeeId: Guid.CreateVersion7()));
    }

    [Fact]
    public void Position_assignment_can_end_without_deleting_history()
    {
        var assignment = PositionAssignment.Create(OrganizationId, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), new DateOnly(2026, 1, 1), null);

        assignment.End(new DateOnly(2026, 10, 24), DateTimeOffset.UtcNow);

        Assert.Equal(new DateOnly(2026, 10, 24), assignment.EffectiveTo);
        Assert.Equal(2, assignment.Version);
    }

    [Fact]
    public void Position_assignment_can_be_corrected_on_its_start_date()
    {
        var positionId = Guid.CreateVersion7();
        var departmentId = Guid.CreateVersion7();
        var assignment = PositionAssignment.Create(OrganizationId, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), new DateOnly(2026, 11, 1), null);

        assignment.Correct(positionId, departmentId, DateTimeOffset.UtcNow);

        Assert.Equal(positionId, assignment.PositionId);
        Assert.Equal(departmentId, assignment.DepartmentId);
        Assert.Equal(2, assignment.Version);
    }

    [Fact]
    public void Reporting_line_can_end_and_be_corrected_without_deleting_history()
    {
        var managerId = Guid.CreateVersion7();
        var reportingLine = ReportingLine.Create(OrganizationId, Guid.CreateVersion7(), Guid.CreateVersion7(),
            new DateOnly(2026, 1, 1), null);

        reportingLine.End(new DateOnly(2026, 10, 31), DateTimeOffset.UtcNow);
        reportingLine.Correct(managerId, DateTimeOffset.UtcNow);

        Assert.Equal(new DateOnly(2026, 10, 31), reportingLine.EffectiveTo);
        Assert.Equal(managerId, reportingLine.ManagerEmployeeId);
        Assert.Equal(3, reportingLine.Version);
    }

    [Fact]
    public void Position_requires_a_department()
    {
        Assert.Throws<DomainException>(() => Position.Create(OrganizationId, Guid.Empty, "Junior Software Engineer", "IT-JUNIOR-SWE"));
    }

    [Fact]
    public void Position_tracks_its_department()
    {
        var itDepartmentId = Guid.CreateVersion7();
        var hrDepartmentId = Guid.CreateVersion7();
        var position = Position.Create(OrganizationId, itDepartmentId, "IT Specialist", "IT-SPECIALIST");

        position.Update(hrDepartmentId, "Junior HR", "HR-JUNIOR", DateTimeOffset.UtcNow);

        Assert.Equal(hrDepartmentId, position.DepartmentId);
        Assert.Equal("Junior HR", position.Title);
        Assert.Equal(2, position.Version);
    }

    [Fact]
    public void Project_knowledge_requires_project_member_audience()
    {
        Assert.Throws<DomainException>(() => KnowledgeArticle.Create(
            OrganizationId, "architecture", Guid.CreateVersion7(), KnowledgeAudience.AllEmployees,
            Guid.CreateVersion7(), null, 0));
    }

    [Fact]
    public void Knowledge_publication_tracks_an_immutable_revision_number()
    {
        var article = KnowledgeArticle.Create(OrganizationId, "company-overview", Guid.CreateVersion7(),
            KnowledgeAudience.AllEmployees, null, null, 0);

        article.SubmitForReview(DateTimeOffset.UtcNow);
        article.Publish(1, DateTimeOffset.UtcNow);

        Assert.Equal(KnowledgeArticleStatus.Published, article.Status);
        Assert.Equal(1, article.PublishedRevision);
        Assert.Equal(3, article.Version);
    }

    [Fact]
    public void Project_membership_rejects_an_end_before_its_start()
    {
        Assert.Throws<DomainException>(() => ProjectMembership.Create(OrganizationId, Guid.CreateVersion7(), Guid.CreateVersion7(),
            "Developer", "Backend responsibilities", new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 9)));
    }

    [Fact]
    public void Published_onboarding_template_cannot_be_edited_in_place()
    {
        var template = OnboardingTemplate.Create(OrganizationId, "Developer onboarding", "Thirty day journey", null, null, 30);
        template.Publish(DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => template.UpdateDraft("Changed", "Changed", null, null, 45, DateTimeOffset.UtcNow));
        Assert.Equal(1, template.PublishedVersion);
    }

    [Fact]
    public void Manager_reviewed_step_becomes_pending_instead_of_self_completed()
    {
        var templateStep = OnboardingTemplateStep.Create(OrganizationId, Guid.CreateVersion7(), "First week", 0,
            "Complete starter task", "Open a pull request", "Reviewed pull request", OnboardingStepType.ManagerReviewedMilestone,
            0, 7, true, null, null, null, null);
        var progress = OnboardingStepProgress.Create(OrganizationId, Guid.CreateVersion7(), templateStep, new DateOnly(2026, 10, 1), null);

        progress.SetProgress(OnboardingStepStatus.Completed, "Ready for review", Guid.CreateVersion7(), true, DateTimeOffset.UtcNow);

        Assert.Equal(OnboardingStepStatus.PendingReview, progress.Status);
        progress.Review(true, Guid.CreateVersion7(), "Approved", DateTimeOffset.UtcNow);
        Assert.Equal(OnboardingStepStatus.Completed, progress.Status);
    }
}
