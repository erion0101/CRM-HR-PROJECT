using Microsoft.AspNetCore.Identity;

namespace HrManagement.Infrastructure;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid OrganizationId { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
}
