using System.Security.Claims;
using HrManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace HrManagement.Infrastructure.Identity;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor, BackgroundJobContext job) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => job.IsActive ? null : accessor.HttpContext?.User;
    public Guid? UserId => job.IsActive ? job.UserId : Parse(ClaimTypes.NameIdentifier);
    public Guid? OrganizationId => job.IsActive ? job.OrganizationId : Parse("organization_id");
    public Guid? EmployeeId => job.IsActive ? null : Parse("employee_id");
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
    public string? CorrelationId => job.IsActive ? job.CorrelationId : accessor.HttpContext?.TraceIdentifier;
    public string? IpAddress => job.IsActive ? null : accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    private Guid? Parse(string type) => Guid.TryParse(Principal?.FindFirstValue(type), out var value) ? value : null;
}
