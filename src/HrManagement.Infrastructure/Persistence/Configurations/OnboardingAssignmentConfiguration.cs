using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingAssignmentConfiguration : EntityConfigurationBase<OnboardingAssignment>
{
    public override void Configure(EntityTypeBuilder<OnboardingAssignment> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.Status });
        builder.HasIndex(x => new { x.EmployeeId, x.TemplateId, x.TemplateVersionNumber });
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingTemplate>().WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingTemplateVersion>().WithMany().HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingTemplate>().WithMany().HasForeignKey(x => x.ProjectTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingTemplateVersion>().WithMany().HasForeignKey(x => x.ProjectTemplateVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.ManagerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.MentorEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
