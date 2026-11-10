using HrManagement.Domain;
using Xunit;

namespace HrManagement.Domain.Tests;

public sealed class OnboardingPackageTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Project_package_must_belong_to_a_project()
    {
        Assert.Throws<DomainException>(() => OnboardingTemplate.Create(OrganizationId, "Portal", "Project docs", null, null, 30,
            OnboardingTemplateKind.Project));

        var package = OnboardingTemplate.Create(OrganizationId, "Portal", "Project docs", null, null, 30,
            OnboardingTemplateKind.Project, Guid.NewGuid());
        Assert.Equal(OnboardingTemplateKind.Project, package.Kind);
    }

    [Fact]
    public void Company_package_cannot_belong_to_a_project()
    {
        Assert.Throws<DomainException>(() => OnboardingTemplate.Create(OrganizationId, "Company", "Company docs", null, null, 30,
            OnboardingTemplateKind.Company, Guid.NewGuid()));
    }

    [Fact]
    public void Existing_templates_default_to_general_and_need_publishing_to_be_assignable()
    {
        var template = OnboardingTemplate.Create(OrganizationId, "Legacy", "Old template", null, null, 30);

        Assert.Equal(OnboardingTemplateKind.General, template.Kind);
        Assert.False(template.IsAssignable);
        template.Publish(DateTimeOffset.UtcNow);
        Assert.True(template.IsAssignable);
        template.BeginNewVersion(DateTimeOffset.UtcNow);
        Assert.True(template.IsAssignable);
    }

    [Fact]
    public void Journey_lists_company_package_before_project_package()
    {
        var companyVersionId = Guid.NewGuid();
        var projectVersionId = Guid.NewGuid();
        var assignment = OnboardingAssignment.Create(OrganizationId, Guid.NewGuid(), Guid.NewGuid(), companyVersionId, 1,
            Guid.NewGuid(), null, null, new DateOnly(2026, 11, 10));

        Assert.Equal([companyVersionId], assignment.PackageVersionIds);
        assignment.IncludeProjectPackage(Guid.NewGuid(), projectVersionId, 2);

        Assert.Equal([companyVersionId, projectVersionId], assignment.PackageVersionIds);
        Assert.Equal(2, assignment.ProjectTemplateVersionNumber);
        Assert.Throws<DomainException>(() => assignment.IncludeProjectPackage(Guid.NewGuid(), Guid.NewGuid(), 1));
    }

    [Fact]
    public void Project_package_requires_a_project_on_the_journey()
    {
        var assignment = OnboardingAssignment.Create(OrganizationId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            null, null, null, new DateOnly(2026, 11, 10));

        Assert.Throws<DomainException>(() => assignment.IncludeProjectPackage(Guid.NewGuid(), Guid.NewGuid(), 1));
    }

    [Fact]
    public void Project_steps_are_ordered_after_company_steps()
    {
        var templateStep = OnboardingTemplateStep.Create(OrganizationId, Guid.NewGuid(), "Project architecture", 0,
            "Architecture overview", "Read it", "Understand it", OnboardingStepType.ReadDocument, 0, 3, true,
            null, Guid.NewGuid(), null, null);

        var step = OnboardingStepProgress.Create(OrganizationId, Guid.NewGuid(), templateStep, new DateOnly(2026, 11, 10), null, 5);

        Assert.Equal(5, step.SectionOrder);
        Assert.Equal(new DateOnly(2026, 11, 13), step.DueDate);
    }
}
