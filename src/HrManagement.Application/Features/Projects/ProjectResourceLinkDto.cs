namespace HrManagement.Application.Features.Projects;

public sealed record ProjectResourceLinkDto(Guid Id, string Label, string Url, string ResourceType, int SortOrder);
