namespace HrManagement.Application.Common.Interfaces;

public interface IDocumentProcessor
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}
