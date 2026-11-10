using System.Collections.Concurrent;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Ai;

internal sealed class AiUsageService(
    AppDbContext db,
    IAiRuntimeSettings settings,
    ICurrentUserService currentUser,
    IDistributedCache cache) : IAiUsageService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserSemaphores = new(StringComparer.Ordinal);

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var options = settings.Value;
        var semaphore = UserSemaphores.GetOrAdd($"{organizationId}:{userId}", _ => new SemaphoreSlim(options.ConcurrentRequestsPerUser, options.ConcurrentRequestsPerUser));
        if (!await semaphore.WaitAsync(TimeSpan.Zero, cancellationToken)) throw Limit("Too many concurrent AI requests.");
        try
        {
            await IncrementAsync($"ai:minute:{organizationId}:{userId}:{DateTimeOffset.UtcNow:yyyyMMddHHmm}", options.RequestsPerMinutePerUser, TimeSpan.FromMinutes(2), cancellationToken);
            await IncrementAsync($"ai:day:user:{organizationId}:{userId}:{DateTimeOffset.UtcNow:yyyyMMdd}", options.RequestsPerDayPerUser, TimeSpan.FromDays(2), cancellationToken);
            await IncrementAsync($"ai:day:org:{organizationId}:{DateTimeOffset.UtcNow:yyyyMMdd}", options.RequestsPerDayPerOrganization, TimeSpan.FromDays(2), cancellationToken);
            var month = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var spend = await db.AiUsageRecords.Where(x => x.OrganizationId == organizationId && x.CreatedAtUtc >= month)
                .SumAsync(x => (decimal?)x.EstimatedCostUsd, cancellationToken) ?? 0;
            if (spend >= options.MonthlyBudgetUsd) throw Limit("The organization's monthly AI budget has been reached.");
            return new Lease(semaphore);
        }
        catch
        {
            semaphore.Release();
            throw;
        }
    }

    public Task RecordAsync(Guid messageId, string model, int inputTokens, int outputTokens, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var options = settings.Value;
        var estimatedCost = (inputTokens / 1_000_000m * options.InputCostPerMillionUsd) +
            (outputTokens / 1_000_000m * options.OutputCostPerMillionUsd);
        db.AiUsageRecords.Add(AiUsageRecord.Create(organizationId, userId, messageId, model, inputTokens, outputTokens, estimatedCost));
        return Task.CompletedTask;
    }

    private async Task IncrementAsync(string key, int limit, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var current = int.TryParse(await cache.GetStringAsync(key, cancellationToken), out var value) ? value : 0;
        if (current >= limit) throw Limit("The AI request limit has been reached. Try again later.");
        await cache.SetStringAsync(key, (current + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime }, cancellationToken);
    }

    private static ValidationException Limit(string message) => new(new Dictionary<string, string[]> { ["rateLimit"] = [message] });

    private sealed class Lease(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() { semaphore.Release(); return ValueTask.CompletedTask; }
    }
}
