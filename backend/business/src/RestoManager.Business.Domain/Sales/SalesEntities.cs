namespace RestoManager.Business.Domain.Sales;

// Módulo Ventas. Solo esquema en la Fase 3. La Fase 6 añade el agregado Order con
// canal explícito, coherencias (ORD-*, DOM-01/02), pagos y descuentos.

public sealed class Order
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int? TableId { get; set; }
    public int EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public int? TableSessionId { get; set; }

    /// <summary>MESA | BARRA | TAKEAWAY | DELIVERY (CHECK en BD).</summary>
    public string Channel { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public sealed class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class Discount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public sealed class OrderDiscount
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int DiscountId { get; set; }
    public decimal AppliedAmount { get; set; }
}
