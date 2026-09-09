namespace RestoManager.Business.Domain.Organization;

/// <summary>Sucursal de un <see cref="Restaurant"/>. Casi todo el dominio cuelga de aquí.</summary>
public sealed class Branch
{
    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public TimeOnly OpeningTime { get; private set; }
    public TimeOnly ClosingTime { get; private set; }

    private Branch() { }

    public Branch(int restaurantId, string name, string address, string phone, string email, TimeOnly opening, TimeOnly closing)
    {
        RestaurantId = restaurantId;
        Update(name, address, phone, email, opening, closing);
    }

    public void Update(string name, string address, string phone, string email, TimeOnly opening, TimeOnly closing)
    {
        Name = OrgGuard.NotBlank(name, nameof(name));
        Address = OrgGuard.NotBlank(address, nameof(address));
        Phone = OrgGuard.Optional(phone);
        Email = OrgGuard.Optional(email);
        OpeningTime = opening;
        ClosingTime = closing;
    }
}
