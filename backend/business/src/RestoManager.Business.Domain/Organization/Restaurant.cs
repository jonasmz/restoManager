namespace RestoManager.Business.Domain.Organization;

internal static class OrgGuard
{
    public static string NotBlank(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"'{name}' es obligatorio.", name)
            : value.Trim();

    public static string Optional(string? value) => value?.Trim() ?? string.Empty;
}

/// <summary>Empresa. Entidad global (sin sucursal).</summary>
public sealed class Restaurant
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string TaxNumber { get; private set; } = string.Empty;

    private Restaurant() { }

    public Restaurant(string name, string address, string phone, string email, string taxNumber)
        => Update(name, address, phone, email, taxNumber);

    public void Update(string name, string address, string phone, string email, string taxNumber)
    {
        Name = OrgGuard.NotBlank(name, nameof(name));
        Address = OrgGuard.NotBlank(address, nameof(address));
        Phone = OrgGuard.Optional(phone);
        Email = OrgGuard.Optional(email);
        TaxNumber = OrgGuard.Optional(taxNumber);
    }
}
