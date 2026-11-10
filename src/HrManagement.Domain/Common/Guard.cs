namespace HrManagement.Domain;

public static class Guard
{
    public static Guid NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty) throw new DomainException($"{name} is required.");
        return value;
    }

    public static string Required(string? value, string name, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new DomainException($"{name} is required.");
        if (normalized.Length > maxLength) throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        return normalized;
    }

    public static void DateRange(DateOnly start, DateOnly? end, string name)
    {
        if (end is not null && end < start) throw new DomainException($"{name} end date cannot be before its start date.");
    }
}
