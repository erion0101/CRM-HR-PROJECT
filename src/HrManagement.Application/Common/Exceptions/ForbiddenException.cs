namespace HrManagement.Application.Common.Exceptions;

public sealed class ForbiddenException(string message = "Access denied.") : Exception(message);
