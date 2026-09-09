namespace RestoManager.Business.Domain.Tax;

// Módulo Fiscal. Solo esquema en la Fase 3. La Fase 4 añade CRUD y el cálculo lo
// usa la Fase 6.

public sealed class TaxRate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}

public sealed class MenuItemTax
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public int TaxRateId { get; set; }
}
