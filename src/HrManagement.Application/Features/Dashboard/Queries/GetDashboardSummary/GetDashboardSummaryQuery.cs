using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;
