using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Positions.Commands.ArchivePosition;

public sealed record ArchivePositionCommand(Guid Id, long ExpectedVersion) : ICommand;
