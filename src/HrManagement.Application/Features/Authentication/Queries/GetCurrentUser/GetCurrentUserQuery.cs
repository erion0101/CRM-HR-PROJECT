using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Authentication.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUser?>;
