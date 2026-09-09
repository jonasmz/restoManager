using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Tax;

// Módulo Fiscal. La Fase 4 añade CRUD de tasas; el cálculo de la cuenta (impuestos
// inclusivos: se desglosan hacia atrás sobre el precio) lo hace la Fase 6.

/// <summary>Tasa de impuesto. Entidad global. <see cref="Rate"/> es un porcentaje (p. ej. 21.00 = 21 %).</summary>
public sealed class TaxRate
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }

    private TaxRate() { }

    public TaxRate(string name, decimal rate) => Update(name, rate);

    public void Update(string name, decimal rate)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("'name' es obligatorio.", nameof(name))
            : name.Trim();

        if (rate < 0 || rate > 100)
        {
            throw new DomainRuleException("fiscal.invalid_rate", "La tasa debe estar entre 0 y 100.");
        }
        Rate = rate;
    }
}

/// <summary>Impuesto aplicado a un plato. Se gestiona como parte del agregado <c>MenuItem</c>.</summary>
public sealed class MenuItemTax
{
    public int Id { get; private set; }
    public int MenuItemId { get; private set; }
    public int TaxRateId { get; private set; }

    private MenuItemTax() { }

    internal static MenuItemTax Create(int taxRateId) => new() { TaxRateId = taxRateId };
}
