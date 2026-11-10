using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class AiConversationRepository(AppDbContext db, ICurrentUserService currentUser) : IAiConversationRepository
{
    public void Add(AiConversation conversation) => db.AiConversations.Add(conversation);
    public void Add(AiMessage message) => db.AiMessages.Add(message);
    public void Add(AiSourceReference source) => db.AiSourceReferences.Add(source);
    public void Add(AiToolExecution execution) => db.AiToolExecutions.Add(execution);

    public Task<AiConversation?> GetOwnedAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        return db.AiConversations.SingleOrDefaultAsync(x => x.Id == conversationId && x.OrganizationId == organizationId && x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<AiConversation>> ListOwnedAsync(CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        return await db.AiConversations.Where(x => x.OrganizationId == organizationId && x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAtUtc).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken) =>
        await db.AiMessages.Where(x => x.ConversationId == conversationId).OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AiSourceReference>> GetSourcesAsync(Guid messageId, CancellationToken cancellationToken) =>
        await db.AiSourceReferences.Where(x => x.MessageId == messageId).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<AiUsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var today = DateTimeOffset.UtcNow.Date;
        var month = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var requests = await db.AiUsageRecords.CountAsync(x => x.OrganizationId == organizationId && x.CreatedAtUtc >= today, cancellationToken);
        var input = await db.AiUsageRecords.Where(x => x.OrganizationId == organizationId && x.CreatedAtUtc >= today).SumAsync(x => (int?)x.InputTokens, cancellationToken) ?? 0;
        var output = await db.AiUsageRecords.Where(x => x.OrganizationId == organizationId && x.CreatedAtUtc >= today).SumAsync(x => (int?)x.OutputTokens, cancellationToken) ?? 0;
        var cost = await db.AiUsageRecords.Where(x => x.OrganizationId == organizationId && x.CreatedAtUtc >= month).SumAsync(x => (decimal?)x.EstimatedCostUsd, cancellationToken) ?? 0;
        return new AiUsageSummaryDto(requests, input, output, cost);
    }
}
