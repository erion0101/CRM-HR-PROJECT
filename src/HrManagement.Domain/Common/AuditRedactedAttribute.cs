namespace HrManagement.Domain;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditRedactedAttribute : Attribute
{
}
