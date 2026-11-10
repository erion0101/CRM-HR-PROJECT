namespace HrManagement.Application.Common.Authorization;

public static class Roles
{
    public const string Employee = "Employee";
    public const string Manager = "Manager";
    public const string Hr = "HR";
    public const string ProductManager = "ProductManager";

    public static readonly IReadOnlyList<string> All = [Employee, Manager, Hr, ProductManager];

    public static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Employee] = "Self-service access to personal HR records, documents, onboarding, knowledge, and assigned projects.",
        [Manager] = "Employee access plus direct-report visibility and responsibility for team onboarding.",
        [Hr] = "Organization-wide HR administration, including people, structure, compensation, documents, users, audit, and AI settings.",
        [ProductManager] = "Project, project-member, and project-knowledge management without access to restricted HR data."
    };
}
