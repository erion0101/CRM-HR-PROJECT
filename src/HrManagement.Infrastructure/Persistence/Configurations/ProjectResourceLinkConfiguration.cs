using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class ProjectResourceLinkConfiguration : EntityConfigurationBase<ProjectResourceLink>
{
    public override void Configure(EntityTypeBuilder<ProjectResourceLink> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Label).HasMaxLength(120);
        builder.Property(x => x.Url).HasMaxLength(2_000);
        builder.Property(x => x.ResourceType).HasMaxLength(80);
        builder.HasIndex(x => new { x.ProjectId, x.SortOrder });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
