namespace HrManagement.Application.Common.Interfaces;

public interface IAuditWriter
{
    Task RecordAccessAsync(string action, string entityType, string entityId, CancellationToken cancellationToken);
}
