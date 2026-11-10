using HrManagement.Application.Features.Ai;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Ai;

internal sealed class AiMaintenanceProcessor(AppDbContext db) : IAiMaintenanceProcessor
{
    public Task<int> PurgeExpiredConversationsAsync(CancellationToken cancellationToken) =>
        db.AiConversations.IgnoreQueryFilters()
            .Where(x => x.ExpiresAtUtc <= DateTimeOffset.UtcNow || x.IsDeleted)
            .ExecuteDeleteAsync(cancellationToken);
}
