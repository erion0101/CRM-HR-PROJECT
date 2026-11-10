namespace HrManagement.Domain;

/// <summary>Lifecycle of a background AI question-generation job for a draft quiz.</summary>
public enum OnboardingQuizGenerationStatus { None = 0, Queued = 1, Running = 2, Completed = 3, Failed = 4 }
