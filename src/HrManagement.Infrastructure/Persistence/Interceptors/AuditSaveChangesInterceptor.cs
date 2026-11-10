using System.Text.Json;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HrManagement.Infrastructure.Persistence.Interceptors;

internal sealed class AuditSaveChangesInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEvents(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddAuditEvents(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void AddAuditEvents(DbContext? context)
    {
        if (context is null) return;
        var entries = context.ChangeTracker.Entries<Entity>().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        foreach (var entry in entries)
        {
            context.Set<AuditEvent>().Add(AuditEvent.Create(entry.Entity.OrganizationId, currentUser.UserId, currentUser.EmployeeId, entry.State.ToString(), entry.Metadata.ClrType.Name, entry.Entity.Id.ToString(), BuildChanges(entry), currentUser.CorrelationId, currentUser.IpAddress));
        }
    }

    private static string? BuildChanges(EntityEntry<Entity> entry)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "Content", "ApiKey" };
        var changes = new Dictionary<string, object?>();
        foreach (var property in entry.Properties.Where(x => !excluded.Contains(x.Metadata.Name) && x.Metadata.PropertyInfo?.IsDefined(typeof(AuditRedactedAttribute), inherit: true) != true))
        {
            if (entry.State == EntityState.Modified && !property.IsModified) continue;
            changes[property.Metadata.Name] = entry.State switch
            {
                EntityState.Added => new { New = property.CurrentValue },
                EntityState.Deleted => new { Old = property.OriginalValue },
                _ => new { Old = property.OriginalValue, New = property.CurrentValue }
            };
        }
        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
    }
}
