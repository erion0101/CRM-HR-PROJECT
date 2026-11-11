using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery(string? Cursor, int Limit = 50, bool UnlinkedOnly = false) : IQuery<PageResult<UserDto>>;
