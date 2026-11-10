namespace HrManagement.Application.Common.Interfaces;

public interface IDocumentUploadPolicy
{
    long MaxUploadBytes { get; }
}
