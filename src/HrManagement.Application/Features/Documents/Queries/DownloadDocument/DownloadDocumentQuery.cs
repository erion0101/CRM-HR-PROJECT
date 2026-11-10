using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Documents.Queries.DownloadDocument;

public sealed record DownloadDocumentQuery(Guid EmployeeId, Guid DocumentId) : IQuery<DocumentDownload>;
