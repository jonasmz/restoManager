using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Sales;

// Módulo Ventas (Fase 6). El agregado `Order` tiene canal explícito (ORD-05), aplica
// la coherencia estructural canal ↔ mesa ↔ sesión (ORD-06, CHECK cruzado del DDL) y
// recalcula `total_amount` ante cada cambio. Las coherencias que dependen de otras
// entidades (DOM-01/02, ORD-03/04) las valida el caso de uso.
// Fase 6a: pedido + ítems + total. Fase 6b: descuentos, pagos múltiples, cierre y
// cancelación. Fase 6c: descuento de stock por receta (evento OPEN → PAID) y reversa.

// ─────────────────────────── Canal ───────────────────────────

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

// ─────────────────────────── Estado del pedido ───────────────────────────

/// <summary>
/// Ciclo del pedido (catálogo aprobado, transversal §2): <c>OPEN → PAID → CLOSED</c>;
/// <c>OPEN/PAID → CANCELLED</c>. <c>PAID</c> = existe ≥1 pago <c>CONFIRMED</c> (el
/// primero dispara el movimiento <c>SALE</c> de la Fase 6c). Venta efectiva
/// (MET-01) = <c>PAID</c> o <c>CLOSED</c>.
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

// ─────────────────────────── Medios y estado de pago ───────────────────────────

/// <summary>Medio de pago (catálogo aprobado, transversal §2). Fijo en la aplicación.</summary>
public enum PaymentMethod
{
    Cash,
    Card,
    Transfer,
    GiftCard,
    Other,
}

public static class PaymentMethodExtensions
{
    public static string ToDbValue(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "CASH",
        PaymentMethod.Card => "CARD",
        PaymentMethod.Transfer => "TRANSFER",
        PaymentMethod.GiftCard => "GIFT_CARD",
        PaymentMethod.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
    };

    public static PaymentMethod FromDbValue(string value) => value switch
    {
        "CASH" => PaymentMethod.Cash,
        "CARD" => PaymentMethod.Card,
        "TRANSFER" => PaymentMethod.Transfer,
        "GIFT_CARD" => PaymentMethod.GiftCard,
        "OTHER" => PaymentMethod.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "payments.payment_method inválido"),
    };

    public static bool TryFromDbValue(string? value, out PaymentMethod method)
    {
        switch (value)
        {
            case "CASH": method = PaymentMethod.Cash; return true;
            case "CARD": method = PaymentMethod.Card; return true;
            case "TRANSFER": method = PaymentMethod.Transfer; return true;
            case "GIFT_CARD": method = PaymentMethod.GiftCard; return true;
            case "OTHER": method = PaymentMethod.Other; return true;
            default: method = default; return false;
        }
    }
}

/// <summary>Estado del pago (catálogo aprobado, transversal §2). Cuenta para métricas = <c>CONFIRMED</c>.</summary>
public enum PaymentStatus
{
    Pending,
    Confirmed,
    Failed,
    Refunded,
}

public static class PaymentStatusExtensions
{
    public static string ToDbValue(this PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "PENDING",
        PaymentStatus.Confirmed => "CONFIRMED",
        PaymentStatus.Failed => "FAILED",
        PaymentStatus.Refunded => "REFUNDED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static PaymentStatus FromDbValue(string value) => value switch
    {
        "PENDING" => PaymentStatus.Pending,
        "CONFIRMED" => PaymentStatus.Confirmed,
        "FAILED" => PaymentStatus.Failed,
        "REFUNDED" => PaymentStatus.Refunded,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "payments.status inválido"),
    };
}

// ─────────────────────────── Descuentos (catálogo global) ───────────────────────────

public enum DiscountType
{
    Percentage,
    FixedAmount,
}

public static class DiscountTypeExtensions
{
    public static string ToDbValue(this DiscountType type) => type switch
    {
        DiscountType.Percentage => "PERCENTAGE",
        DiscountType.FixedAmount => "FIXED_AMOUNT",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static DiscountType FromDbValue(string value) => value switch
    {
        "PERCENTAGE" => DiscountType.Percentage,
        "FIXED_AMOUNT" => DiscountType.FixedAmount,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "discounts.type inválido"),
    };

    public static bool TryFromDbValue(string? value, out DiscountType type)
    {
        switch (value)
        {
            case "PERCENTAGE": type = DiscountType.Percentage; return true;
            case "FIXED_AMOUNT": type = DiscountType.FixedAmount; return true;
            default: type = default; return false;
        }
    }
}

/// <summary>
/// Descuento del catálogo (entidad global, sin sucursal). <see cref="Value"/> es el
/// porcentaje (0-100) para <see cref="DiscountType.Percentage"/> o el importe fijo
/// para <see cref="DiscountType.FixedAmount"/>.
/// </summary>
public sealed class Discount
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DiscountType Type { get; private set; }
    public decimal Value { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    private Discount() { }

    public Discount(string name, DiscountType type, decimal value, DateOnly startDate, DateOnly endDate)
        => Update(name, type, value, startDate, endDate);

    public void Update(string name, DiscountType type, decimal value, DateOnly startDate, DateOnly endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("sales.discount_name_required", "El nombre del descuento es obligatorio.");
        }
        if (value <= 0m)
        {
            throw new DomainRuleException("sales.discount_invalid_value", "El valor del descuento debe ser mayor que cero.");
        }
        if (type == DiscountType.Percentage && value > 100m)
        {
            throw new DomainRuleException("sales.discount_invalid_percentage", "Un descuento porcentual no puede superar el 100 %.");
        }
        if (endDate < startDate)
        {
            throw new DomainRuleException("sales.discount_invalid_range", "La fecha de fin no puede ser anterior a la de inicio.");
        }

        Name = name.Trim();
        Type = type;
        Value = value;
        StartDate = startDate;
        EndDate = endDate;
    }

    public bool IsActiveOn(DateOnly date) => date >= StartDate && date <= EndDate;

    /// <summary>Importe bruto del descuento sobre <paramref name="itemsSubtotal"/> (sin topar).</summary>
    public decimal ComputeApplied(decimal itemsSubtotal) => Type switch
    {
        DiscountType.Percentage => Money.Round(itemsSubtotal * Value / 100m),
        DiscountType.FixedAmount => Value,
        _ => 0m,
    };
}

// ─────────────────────────── Pedido ───────────────────────────

/// <summary>
/// Pedido: raíz del agregado. Agrega sus <see cref="OrderItem"/>, sus
/// <see cref="OrderDiscount"/> y sus <see cref="Payment"/>. Se abre en
/// <see cref="OrderStatus.Open"/>; los ítems y descuentos solo se editan en ese
/// estado. El primer pago <c>CONFIRMED</c> lo pasa a <see cref="OrderStatus.Paid"/>.
/// </summary>
public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderDiscount> _discounts = [];
    private readonly List<Payment> _payments = [];

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
    public IReadOnlyList<OrderDiscount> Discounts => _discounts;
    public IReadOnlyList<Payment> Payments => _payments;

    /// <summary>Σ de las líneas, redondeado. Derivado (no se persiste).</summary>
    public decimal ItemsSubtotal => Money.Round(_items.Sum(i => i.LineTotal));

    /// <summary>Σ de los descuentos aplicados, redondeado. Derivado.</summary>
    public decimal DiscountTotal => Money.Round(_discounts.Sum(d => d.AppliedAmount));

    /// <summary>Σ de los pagos <c>CONFIRMED</c>, redondeado. Derivado.</summary>
    public decimal ConfirmedPaid =>
        Money.Round(_payments.Where(p => p.Status == PaymentStatus.Confirmed).Sum(p => p.Amount));

    /// <summary>Saldo pendiente de cobro (<see cref="TotalAmount"/> − <see cref="ConfirmedPaid"/>).</summary>
    public decimal Balance => Money.Round(TotalAmount - ConfirmedPaid);

    private Order() { }

    /// <summary>
    /// Crea el pedido con canal fijo. ORD-06 / <c>CHECK</c> cruzado del DDL: canal
    /// <c>MESA</c> ⇒ <c>table_id</c> obligatorio; cualquier otro canal ⇒ <c>table_id</c>
    /// y <c>table_session_id</c> nulos.
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

    // ---- Ítems ----

    /// <summary>
    /// Agrega un plato al pedido. Si ya existe una línea con el <b>mismo plato y la misma
    /// nota</b>, incrementa su cantidad en lugar de crear una segunda línea (el precio
    /// unitario de esa línea se mantiene, capturado al primer agregado). Distinta nota =
    /// línea aparte.
    /// </summary>
    public OrderItem AddItem(int menuItemId, int quantity, decimal unitPrice, string? notes)
    {
        EnsureOpen("agregar ítems");

        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        var existing = _items.FirstOrDefault(i => i.MenuItemId == menuItemId && i.Notes == normalizedNotes);
        if (existing is not null)
        {
            existing.Update(existing.Quantity + quantity, existing.Notes);
            Recalculate();
            return existing;
        }

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

    // ---- Descuentos ----

    /// <summary>
    /// Aplica un descuento del catálogo. El descuento debe estar vigente en
    /// <paramref name="onDate"/> y no puede repetirse (<c>UNIQUE(order_id, discount_id)</c>).
    /// El importe se congela al aplicar y se topa para que el total no baje de 0.
    /// </summary>
    public OrderDiscount ApplyDiscount(Discount discount, DateOnly onDate)
    {
        EnsureOpen("aplicar descuentos");

        if (!discount.IsActiveOn(onDate))
        {
            throw new DomainRuleException(
                "sales.discount_not_active", $"El descuento '{discount.Name}' no está vigente.");
        }
        if (_discounts.Any(d => d.DiscountId == discount.Id))
        {
            throw new DomainRuleException(
                "sales.discount_duplicate", "El descuento ya está aplicado a este pedido.");
        }

        var remaining = Money.Round(ItemsSubtotal - DiscountTotal);
        var applied = Math.Clamp(discount.ComputeApplied(ItemsSubtotal), 0m, remaining < 0m ? 0m : remaining);

        var row = OrderDiscount.Create(discount.Id, applied);
        _discounts.Add(row);
        Recalculate();
        return row;
    }

    public void RemoveDiscount(int discountId)
    {
        EnsureOpen("quitar descuentos");
        var row = _discounts.FirstOrDefault(d => d.DiscountId == discountId)
            ?? throw new NotFoundException("descuento del pedido", discountId);
        _discounts.Remove(row);
        Recalculate();
    }

    /// <summary>
    /// Canje de puntos de fidelidad como descuento del pedido (Fase 8, decisión 1).
    /// El importe ya viene calculado por el caso de uso (<c>puntos / ratio</c>) y se
    /// congela en <see cref="OrderDiscount.AppliedAmount"/>. Se apoya en la fila de
    /// <c>discounts</c> de sistema (<paramref name="loyaltyDiscountId"/>) para respetar
    /// la FK; solo se admite un canje por pedido (<c>UNIQUE(order_id, discount_id)</c>).
    /// </summary>
    public OrderDiscount ApplyLoyaltyRedemption(int loyaltyDiscountId, decimal amount)
    {
        EnsureOpen("canjear puntos de fidelidad");

        if (amount <= 0m)
        {
            throw new DomainRuleException(
                "loyalty.invalid_redemption", "El importe del canje debe ser mayor que cero.");
        }
        if (_discounts.Any(d => d.DiscountId == loyaltyDiscountId))
        {
            throw new DomainRuleException(
                "loyalty.already_redeemed", "El pedido ya tiene un canje de puntos aplicado.");
        }

        var remaining = Money.Round(ItemsSubtotal - DiscountTotal);
        if (amount > remaining)
        {
            throw new DomainRuleException(
                "loyalty.redemption_exceeds_total",
                $"El canje ({amount:0.00}) supera el importe pendiente del pedido ({remaining:0.00}).");
        }

        var row = OrderDiscount.Create(loyaltyDiscountId, Money.Round(amount));
        _discounts.Add(row);
        Recalculate();
        return row;
    }

    // ---- Pagos ----

    /// <summary>
    /// Registra un pago <c>CONFIRMED</c>. Σ pagos confirmados no puede superar el total
    /// (§5.2, permite split). El primer pago confirmado pasa el pedido a <c>PAID</c>.
    /// </summary>
    public Payment RegisterPayment(PaymentMethod method, decimal amount, DateTime now)
    {
        if (Status is not (OrderStatus.Open or OrderStatus.Paid))
        {
            throw new DomainRuleException(
                "sales.order_not_payable",
                $"No se puede registrar un pago en un pedido en estado {Status.ToDbValue()}.");
        }
        if (amount <= 0m)
        {
            throw new DomainRuleException("sales.payment_invalid_amount", "El importe del pago debe ser mayor que cero.");
        }
        if (TotalAmount <= 0m)
        {
            throw new DomainRuleException("sales.payment_zero_total", "El pedido no tiene importe a cobrar.");
        }
        if (Money.Round(ConfirmedPaid + amount) > TotalAmount)
        {
            throw new DomainRuleException(
                "sales.payment_exceeds_total", "La suma de los pagos superaría el total del pedido.");
        }

        var payment = Payment.Create(method, amount, now);
        _payments.Add(payment);

        if (Status == OrderStatus.Open)
        {
            Status = OrderStatus.Paid; // primer pago confirmado (evento del SALE en Fase 6c)
        }
        return payment;
    }

    // ---- Cierre / cancelación ----

    /// <summary>Cierra el pedido. Requiere estar totalmente pagado (o total 0).</summary>
    public void CloseOrder()
    {
        if (Status == OrderStatus.Closed)
        {
            return;
        }
        if (Status is OrderStatus.Cancelled)
        {
            throw new DomainRuleException("sales.order_cancelled", "El pedido está cancelado.");
        }
        if (TotalAmount > 0m && ConfirmedPaid < TotalAmount)
        {
            throw new DomainRuleException(
                "sales.order_underpaid", "No se puede cerrar un pedido que no está totalmente pagado.");
        }
        Status = OrderStatus.Closed;
    }

    /// <summary>
    /// Cancela el pedido (desde <c>OPEN</c> o <c>PAID</c>). Los pagos <c>CONFIRMED</c>
    /// pasan a <c>REFUNDED</c>. Si estaba <c>PAID</c>, la Fase 6c revierte el stock.
    /// </summary>
    public void CancelOrder()
    {
        if (Status == OrderStatus.Cancelled)
        {
            return;
        }
        if (Status == OrderStatus.Closed)
        {
            throw new DomainRuleException("sales.order_closed", "No se puede cancelar un pedido ya cerrado.");
        }
        foreach (var payment in _payments.Where(p => p.Status == PaymentStatus.Confirmed))
        {
            payment.Refund();
        }
        Status = OrderStatus.Cancelled;
    }

    // ---- Interno ----

    /// <summary>
    /// <c>total_amount = Σ(líneas) − Σ(descuentos)</c> (los impuestos ya están incluidos
    /// en <see cref="OrderItem.UnitPrice"/>, decisión Fase 4), redondeado medio-arriba a
    /// 2 decimales y nunca por debajo de 0.
    /// </summary>
    public void Recalculate()
    {
        var total = ItemsSubtotal - DiscountTotal;
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

/// <summary>Descuento aplicado a un pedido. <see cref="AppliedAmount"/> se congela al aplicar.</summary>
public sealed class OrderDiscount
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int DiscountId { get; private set; }
    public decimal AppliedAmount { get; private set; }

    private OrderDiscount() { }

    internal static OrderDiscount Create(int discountId, decimal appliedAmount)
    {
        if (discountId <= 0)
        {
            throw new DomainRuleException("sales.invalid_discount", "El descuento es obligatorio.");
        }
        return new OrderDiscount { DiscountId = discountId, AppliedAmount = appliedAmount < 0m ? 0m : appliedAmount };
    }
}

/// <summary>Pago de un pedido. Se crea <c>CONFIRMED</c>; se revierte a <c>REFUNDED</c> al cancelar.</summary>
public sealed class Payment
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentTime { get; private set; }
    public PaymentStatus Status { get; private set; }

    private Payment() { }

    internal static Payment Create(PaymentMethod method, decimal amount, DateTime now)
    {
        if (amount <= 0m)
        {
            throw new DomainRuleException("sales.payment_invalid_amount", "El importe del pago debe ser mayor que cero.");
        }
        return new Payment
        {
            PaymentMethod = method,
            Amount = amount,
            PaymentTime = now,
            Status = PaymentStatus.Confirmed,
        };
    }

    internal void Refund()
    {
        if (Status != PaymentStatus.Confirmed)
        {
            return;
        }
        Status = PaymentStatus.Refunded;
    }
}
