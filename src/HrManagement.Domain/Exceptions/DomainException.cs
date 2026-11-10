namespace HrManagement.Domain;

public sealed class DomainException(string message) : Exception(message);
