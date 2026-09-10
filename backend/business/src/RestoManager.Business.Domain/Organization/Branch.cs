using System.Text.RegularExpressions;
using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Organization;

/// <summary>Sucursal de un <see cref="Restaurant"/>. Casi todo el dominio cuelga de aquí.</summary>
public sealed partial class Branch
{
    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{1,58}[a-z0-9])$")]
    private static partial Regex SlugPattern();

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public TimeOnly OpeningTime { get; private set; }
    public TimeOnly ClosingTime { get; private set; }

    /// <summary>
    /// Identificador público para la carta accesible por QR (Fase 11). <c>null</c> = la
    /// sucursal no publica carta. Formato: <c>[a-z0-9-]</c>, 3–60, sin guion al borde.
    /// </summary>
    public string? PublicSlug { get; private set; }

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

    /// <summary>
    /// Fija (o quita, con <c>null</c>/vacío) el slug público de la carta. Normaliza a
    /// minúsculas y valida el formato; la unicidad la comprueba la capa de aplicación.
    /// </summary>
    public void SetPublicSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            PublicSlug = null;
            return;
        }

        var normalized = slug.Trim().ToLowerInvariant();
        if (!SlugPattern().IsMatch(normalized))
        {
            throw new DomainRuleException(
                "branch.invalid_slug",
                "El slug debe tener entre 3 y 60 caracteres [a-z, 0-9, -] y no empezar ni terminar con guion.");
        }
        PublicSlug = normalized;
    }
}
