using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingTemplateVersionConfiguration : EntityConfigurationBase<OnboardingTemplateVersion>
{
    public override void Configure(EntityTypeBuilder<OnboardingTemplateVersion> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique();
        builder.HasOne<OnboardingTemplate>().WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
    }
}
