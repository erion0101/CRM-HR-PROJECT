using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class ProjectMembershipConfiguration : EntityConfigurationBase<ProjectMembership>
{
    public override void Configure(EntityTypeBuilder<ProjectMembership> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.ProjectRole).HasMaxLength(120);
        builder.Property(x => x.Responsibilities).HasMaxLength(1_000);
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.EffectiveFrom });
        builder.HasIndex(x => new { x.ProjectId, x.EmployeeId, x.EffectiveFrom }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
