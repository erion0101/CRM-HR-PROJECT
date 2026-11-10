namespace HrManagement.Application.Common.Models;

public sealed record PageResult<T>(IReadOnlyList<T> Items, string? NextCursor);
