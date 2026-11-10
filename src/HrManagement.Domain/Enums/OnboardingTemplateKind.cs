namespace HrManagement.Domain;

/// <summary>
/// What an onboarding template covers. A Company package is assigned to every new employee; a Project package
/// belongs to exactly one project and is added automatically when the employee is onboarded onto that project.
/// General is the original standalone template kind and remains assignable on its own.
/// </summary>
public enum OnboardingTemplateKind { General = 0, Company = 1, Project = 2 }
