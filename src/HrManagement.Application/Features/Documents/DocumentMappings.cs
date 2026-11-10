using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents;

public static class DocumentMappings
{
    public static DocumentDto ToDto(this EmployeeDocument document) => new(document.Id, document.FolderId, document.DisplayName, document.DocumentType, document.Status, document.CurrentVersion, document.Version);
}
