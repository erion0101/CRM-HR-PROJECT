using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Positions.Commands.UpdatePosition;

public sealed record UpdatePositionCommand(Guid Id, Guid DepartmentId, string Title, string? Code, long ExpectedVersion) : ICommand<PositionDto>;
