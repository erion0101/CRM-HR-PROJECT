using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Documents.Commands.ArchiveDocument;

public sealed record ArchiveDocumentCommand(Guid EmployeeId, Guid DocumentId, long ExpectedVersion) : ICommand;
