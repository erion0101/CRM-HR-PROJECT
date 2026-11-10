using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Common.Interfaces;

public interface IDocumentScanner
{
    Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken);
}
