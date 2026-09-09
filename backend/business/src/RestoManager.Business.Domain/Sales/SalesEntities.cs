using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Sales;

// Módulo Ventas (Fase 6). El agregado `Order` tiene canal explícito (ORD-05), aplica
// la coherencia estructural canal ↔ mesa ↔ sesión (ORD-06, CHECK cruzado del DDL) y
// recalcula `total_amount` ante cada cambio. Las coherencias que dependen de otras
// entidades (DOM-01/02, ORD-03/04) las valida el caso de uso. Pagos y descuentos
// llegan en la Fase 6b; el descuento de stock por receta, en la 6c.

/// <summary>Canal del pedido. Fijo por <c>CHECK</c> en la BD.</summary>
public enum OrderChannel
{
    Mesa,
    Barra,
    Takeaway,
    Delivery,
}

public static class OrderChannelExtensions
{
    public static string ToDbValue(this OrderChannel channel) => channel switch
    {
        OrderChannel.Mesa => "MESA",
        OrderChannel.Barra => "BARRA",
        OrderChannel.Takeaway => "TAKEAWAY",
        OrderChannel.Delivery => "DELIVERY",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    public static OrderChannel FromDbValue(string value) => value switch
    {
        "MESA" => OrderChannel.Mesa,
        "BARRA" => OrderChannel.Barra,
        "TAKEAWAY" => OrderChannel.Takeaway,
        "DELIVERY" => OrderChannel.Delivery,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "orders.channel inválido"),
    };

    public static bool TryFromDbValue(string? value, out OrderChannel channel)
    {
        switch (value)
        {
            case "MESA": channel = OrderChannel.Mesa; return true;
            case "BARRA": channel = OrderChannel.Barra; return true;
            case "TAKEAWAY": channel = OrderChannel.Takeaway; return true;
            case "DELIVERY": channel = OrderChannel.Delivery; return true;
            default: channel = default; return false;
        }
    }
}

/// <summary>
/// Ciclo del pedido (catálogo aprobado, transversal §2): <c>OPEN → PAID → CLOSED</c>;
/// <c>OPEN/PAID → CANCELLED</c>. <c>PAID</c> = existe ≥1 pago <c>CONFIRMED</c> (la
/// transición y el disparo del movimiento <c>SALE</c> los gestiona la Fase 6b/6c).
/// Venta efectiva (MET-01) = <c>PAID</c> o <c>CLOSED</c>.
/// </summary>
public enum OrderStatus
{
    Open,
    Paid,
    Closed,
    Cancelled,
}

public static class OrderStatusExtensions
{
    public static string ToDbValue(this OrderStatus status) => status switch
    {
        OrderStatus.Open => "OPEN",
        OrderStatus.Paid => "PAID",
        OrderStatus.Closed => "CLOSED",
        OrderStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static OrderStatus FromDbValue(string value) => value switch
    {
        "OPEN" => OrderStatus.Open,
        "PAID" => OrderStatus.Paid,
        "CLOSED" => OrderStatus.Closed,
        "CANCELLED" => OrderStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "orders.status inválido"),
    };

    public static bool TryFromDbValue(string? value, out OrderStatus status)
    {
        switch (value)
        {
            case "OPEN": status = OrderStatus.Open; return true;
            case "PAID": status = OrderStatus.Paid; return true;
            case "CLOSED": status = OrderStatus.Closed; return true;
            case "CANCELLED": status = OrderStatus.Cancelled; return true;
            default: status = default; return false;
        }
    }
}

/// <summary>
/// Pedido: raíz del agregado. Agrega sus <see cref="OrderItem"/>. Se abre en
/// <see cref="OrderStatus.Open"/> y solo en ese estado admite cambios de ítems.
/// </summary>
public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public int? TableId { get; private set; }
    public int EmployeeId { get; private set; }
    public int? CustomerId { get; private set; }
    public int? TableSessionId { get; private set; }
    public OrderChannel Channel { get; private set; }
    public DateTime OrderTime { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;

    private Order() { }

    /// <summary>
    /// Crea el pedido con canal fijo. ORD-06 / <c>CHECK</c> cruzado del DDL: canal
    /// <c>MESA</c> ⇒ <c>table_id</c> obligatorio; cualquier otro canal ⇒ <c>table_id</c>
    /// y <c>table_session_id</c> nulos. Las coherencias con otras entidades
    /// (sucursal de la mesa/empleado — DOM-01/ORD-04; sesión de esa mesa — DOM-02/ORD-03)
    /// las valida el caso de uso.
    /// </summary>
    public static Order Create(
        OrderChannel channel,
        int branchId,
        int employeeId,
        DateTime now,
        int? tableId,
        int? tableSessionId,
        int? customerId)
    {
        if (branchId <= 0)
        {
            throw new DomainRuleException("sales.invalid_branch", "La sucursal es obligatoria.");
        }
        if (employeeId <= 0)
        {
            throw new DomainRuleException("sales.invalid_employee", "El empleado es obligatorio.");
        }

        if (channel == OrderChannel.Mesa)
        {
            if (tableId is null or <= 0)
            {
                throw new DomainRuleException(
                    "sales.table_required", "Un pedido de canal MESA requiere una mesa.");
            }
        }
        else if (tableId is not null || tableSessionId is not null)
        {
            throw new DomainRuleException(
                "sales.table_not_allowed",
                $"Un pedido de canal {channel.ToDbValue()} no puede tener mesa ni sesión de mesa.");
        }

        return new Order
        {
            Channel = channel,
            BranchId = branchId,
            EmployeeId = employeeId,
            TableId = channel == OrderChannel.Mesa ? tableId : null,
            TableSessionId = channel == OrderChannel.Mesa ? tableSessionId : null,
            CustomerId = customerId,
            OrderTime = now,
            Status = OrderStatus.Open,
            TotalAmount = 0m,
        };
    }

    public OrderItem AddItem(int menuItemId, int quantity, decimal unitPrice, string? notes)
    {
        EnsureOpen("agregar ítems");
        var item = OrderItem.Create(menuItemId, quantity, unitPrice, notes);
        _items.Add(item);
        Recalculate();
        return item;
    }

    public void UpdateItem(int orderItemId, int quantity, string? notes)
    {
        EnsureOpen("modificar ítems");
        FindItem(orderItemId).Update(quantity, notes);
        Recalculate();
    }

    public void RemoveItem(int orderItemId)
    {
        EnsureOpen("quitar ítems");
        _items.Remove(FindItem(orderItemId));
        Recalculate();
    }

    /// <summary>
    /// <c>total_amount = Σ(líneas) − descuentos</c> (los impuestos ya están incluidos en
    /// <see cref="OrderItem.UnitPrice"/>, decisión Fase 4), redondeado medio-arriba a 2
    /// decimales y nunca por debajo de 0. La Fase 6b pasa el total de descuentos.
    /// </summary>
    public void Recalculate(decimal discountTotal = 0m)
    {
        var subtotal = Money.Round(_items.Sum(i => i.LineTotal));
        var total = subtotal - Money.Round(discountTotal);
        TotalAmount = total < 0m ? 0m : Money.Round(total);
    }

    private OrderItem FindItem(int orderItemId) =>
        _items.FirstOrDefault(i => i.Id == orderItemId)
        ?? throw new NotFoundException("ítem de pedido", orderItemId);

    private void EnsureOpen(string action)
    {
        if (Status != OrderStatus.Open)
        {
            throw new DomainRuleException(
                "sales.order_not_open",
                $"No se puede {action} en un pedido en estado {Status.ToDbValue()}.");
        }
    }
}

/// <summary>Línea de pedido. <see cref="UnitPrice"/> se captura al agregar (precio vigente del plato).</summary>
public sealed class OrderItem
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int MenuItemId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Total de la línea, redondeado. No se persiste (se deriva).</summary>
    public decimal LineTotal => Money.Round(Quantity * UnitPrice);

    private OrderItem() { }

    internal static OrderItem Create(int menuItemId, int quantity, decimal unitPrice, string? notes)
    {
        if (menuItemId <= 0)
        {
            throw new DomainRuleException("sales.invalid_menu_item", "El plato es obligatorio.");
        }
        if (unitPrice < 0m)
        {
            throw new DomainRuleException("sales.invalid_unit_price", "El precio no puede ser negativo.");
        }

        var item = new OrderItem { MenuItemId = menuItemId, UnitPrice = unitPrice };
        item.Update(quantity, notes);
        return item;
    }

    internal void Update(int quantity, string? notes)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("sales.invalid_quantity", "La cantidad debe ser mayor que cero.");
        }
        Quantity = quantity;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}

// ─────────── Pagos y descuentos: solo esquema hasta la Fase 6b ───────────

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
