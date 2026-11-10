namespace HrManagement.Domain;

public sealed class Organization : SoftDeletableEntity
{
    private Organization() { }
    public string Name { get; private set; } = string.Empty;
    public string DefaultCurrency { get; private set; } = "EUR";

    public static Organization Create(string name, string currency = "EUR")
    {
        var organization = new Organization { Name = Guard.Required(name, nameof(name), 200) };
        organization.OrganizationId = organization.Id;
        organization.DefaultCurrency = Guard.Required(currency, nameof(currency), 3).ToUpperInvariant();
        return organization;
    }
}
