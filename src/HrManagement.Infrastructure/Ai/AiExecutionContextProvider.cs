using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;

namespace HrManagement.Infrastructure.Ai;

internal sealed class AiExecutionContextProvider(
    ICurrentUserService currentUser,
    IAuthorizedDataScope dataScope,
    IAiRuntimeSettings settings) : IAiExecutionContextProvider
{
    public async Task<AiExecutionContext> GetAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.OrganizationTimeZone);
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        return new AiExecutionContext(
            userId,
            organizationId,
            currentUser.EmployeeId,
            Roles.All.Where(currentUser.IsInRole).Order(StringComparer.Ordinal).ToArray(),
            await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken),
            currentUser.CorrelationId ?? Guid.CreateVersion7().ToString(),
            DateOnly.FromDateTime(localNow.DateTime),
            timeZone.Id);
    }
}
