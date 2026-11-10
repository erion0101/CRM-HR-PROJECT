using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Documents.Commands.UploadDocumentVersion;

public sealed record UploadDocumentVersionCommand(Guid EmployeeId, Guid DocumentId, DocumentUpload Upload, long ExpectedVersion) : ICommand<DocumentDto>;
