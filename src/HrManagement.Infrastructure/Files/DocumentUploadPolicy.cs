using HrManagement.Application.Common.Interfaces;
using HrManagement.Infrastructure.Files.Options;
using Microsoft.Extensions.Options;

namespace HrManagement.Infrastructure.Files;

internal sealed class DocumentUploadPolicy(IOptions<StorageOptions> options) : IDocumentUploadPolicy
{
    public long MaxUploadBytes => options.Value.MaxUploadBytes;
}
