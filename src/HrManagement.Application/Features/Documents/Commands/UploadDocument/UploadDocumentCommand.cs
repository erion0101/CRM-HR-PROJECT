using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Documents.Commands.UploadDocument;

public sealed record UploadDocumentCommand(Guid EmployeeId, DocumentUpload Upload, Guid? FolderId = null) : ICommand<DocumentDto>;
