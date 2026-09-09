namespace RestoManager.Business.Domain.Purchasing;

/// <summary>Proveedor de materias primas.</summary>
public sealed class Supplier
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ContactName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;

    private Supplier() { }

    public Supplier(string name, string contactName, string phone, string email, string address)
    {
        Update(name, contactName, phone, email, address);
    }

    public void Update(string name, string contactName, string phone, string email, string address)
    {
        Name = Require(name, nameof(name));
        ContactName = contactName?.Trim() ?? string.Empty;
        Phone = phone?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"'{name}' es obligatorio.", name)
            : value.Trim();
}
