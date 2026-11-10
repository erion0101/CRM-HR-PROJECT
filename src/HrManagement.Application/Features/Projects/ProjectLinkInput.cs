namespace HrManagement.Application.Features.Projects;

public sealed record ProjectLinkInput(string Label, string Url, string ResourceType, int SortOrder);
