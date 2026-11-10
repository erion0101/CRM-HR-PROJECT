using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Positions.Commands.CreatePosition;

public sealed record CreatePositionCommand(Guid DepartmentId, string Title, string? Code) : ICommand<PositionDto>;
