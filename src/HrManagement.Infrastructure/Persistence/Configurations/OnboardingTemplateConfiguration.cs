using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingTemplateConfiguration : EntityConfigurationBase<OnboardingTemplate>
{
    public override void Configure(EntityTypeBuilder<OnboardingTemplate> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(1_000);
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.Name });
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Position>().WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OrganizationId, x.Kind, x.ProjectId });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }
}
