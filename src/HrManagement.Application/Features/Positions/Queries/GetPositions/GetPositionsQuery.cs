using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Positions.Queries.GetPositions;

public sealed record GetPositionsQuery(string? Cursor, int Limit = 50) : IQuery<PageResult<PositionDto>>;
