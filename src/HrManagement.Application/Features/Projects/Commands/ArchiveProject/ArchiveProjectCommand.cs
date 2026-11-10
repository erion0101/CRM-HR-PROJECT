using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Commands.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid Id, long ExpectedVersion) : ICommand;
