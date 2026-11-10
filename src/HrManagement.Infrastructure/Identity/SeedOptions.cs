namespace HrManagement.Infrastructure.Identity;

public sealed class SeedOptions
{
    public const string Section = "Seed";
    public string OrganizationName { get; set; } = "Local Organization";
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string DemoUsersPassword { get; set; } = string.Empty;
}
