using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Purchasing;

/// <summary>Estado de una orden de compra (borrador de catálogo aprobado en Fase 3).</summary>
public enum PurchaseOrderStatus
{
    Draft,
    Sent,
    PartiallyReceived,
    Received,
    Cancelled,
}

public static class PurchaseOrderStatusExtensions
{
    public static string ToDbValue(this PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "DRAFT",
        PurchaseOrderStatus.Sent => "SENT",
        PurchaseOrderStatus.PartiallyReceived => "PARTIALLY_RECEIVED",
        PurchaseOrderStatus.Received => "RECEIVED",
        PurchaseOrderStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static PurchaseOrderStatus FromDbValue(string value) => value switch
    {
        "DRAFT" => PurchaseOrderStatus.Draft,
        "SENT" => PurchaseOrderStatus.Sent,
        "PARTIALLY_RECEIVED" => PurchaseOrderStatus.PartiallyReceived,
        "RECEIVED" => PurchaseOrderStatus.Received,
        "CANCELLED" => PurchaseOrderStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "purchase_orders.status inválido"),
    };
}

/// <summary>
/// Cabecera de orden de compra. Crearla NO mueve stock (INV-07); el movimiento
/// PURCHASE se genera al recibirla. Fase 3: solo recepción total.
/// </summary>
public sealed class PurchaseOrder
{
    private readonly List<PurchaseOrderItem> _items = [];

    public int Id { get; private set; }
    public int SupplierId { get; private set; }
    public int BranchId { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }

    public IReadOnlyList<PurchaseOrderItem> Items => _items;

    private PurchaseOrder() { }

    public static PurchaseOrder Draft(
        int supplierId, int branchId, DateOnly orderDate, IEnumerable<(int ingredientId, decimal quantity, decimal unitPrice)> lines)
    {
        var order = new PurchaseOrder
        {
            SupplierId = supplierId,
            BranchId = branchId,
            OrderDate = orderDate,
            Status = PurchaseOrderStatus.Draft,
        };

        foreach (var (ingredientId, quantity, unitPrice) in lines)
        {
            order._items.Add(PurchaseOrderItem.Create(ingredientId, quantity, unitPrice));
        }

        if (order._items.Count == 0)
        {
            throw new DomainRuleException("purchasing.empty_order", "La orden de compra debe tener al menos un ítem.");
        }

        order.RecalculateTotal();
        return order;
    }

    public void Send()
    {
        Require(PurchaseOrderStatus.Draft, "enviar");
        Status = PurchaseOrderStatus.Sent;
    }

    public void MarkReceived()
    {
        if (Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new DomainRuleException(
                "purchasing.invalid_transition",
                $"No se puede recibir una orden en estado {Status.ToDbValue()}.");
        }
        Status = PurchaseOrderStatus.Received;
    }

    public void Cancel()
    {
        if (Status == PurchaseOrderStatus.Received)
        {
            throw new DomainRuleException("purchasing.invalid_transition", "No se puede cancelar una orden ya recibida.");
        }
        Status = PurchaseOrderStatus.Cancelled;
    }

    private void RecalculateTotal() => TotalAmount = _items.Sum(i => i.Quantity * i.UnitPrice);

    private void Require(PurchaseOrderStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainRuleException(
                "purchasing.invalid_transition",
                $"No se puede {action} una orden en estado {Status.ToDbValue()}.");
        }
    }
}

public sealed class PurchaseOrderItem
{
    public int Id { get; private set; }
    public int PurchaseOrderId { get; private set; }
    public int IngredientId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private PurchaseOrderItem() { }

    internal static PurchaseOrderItem Create(int ingredientId, decimal quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("purchasing.invalid_quantity", "La cantidad del ítem debe ser mayor que cero.");
        }
        if (unitPrice < 0)
        {
            throw new DomainRuleException("purchasing.invalid_price", "El precio unitario no puede ser negativo.");
        }

        return new PurchaseOrderItem
        {
            IngredientId = ingredientId,
            Quantity = quantity,
            UnitPrice = unitPrice,
        };
    }
}
