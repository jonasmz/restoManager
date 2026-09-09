using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Application.Sales.Consumption;

/// <summary>
/// Descuento de stock por receta (§7.5, §11.5). El evento de dominio elegido (decisión
/// Fase 6) es la transición <c>OPEN → PAID</c> del pedido: al confirmarse el primer
/// pago se expanden los <c>order_items</c> por sus <c>recipe_items</c>, se agrupan por
/// ingrediente y se postean movimientos <c>SALE</c> vía el <see cref="InventoryLedger"/>.
/// Idempotente por <c>reference_type = 'ORDER'</c> + <c>reference_id = order.id</c>
/// (DOM-07): los pagos posteriores de un split no vuelven a descontar. La cancelación
/// de un pedido ya contabilizado genera la reversa (nunca se borran movimientos).
/// NO hace <c>SaveChanges</c>: el caso de uso lo envuelve en la misma transacción.
/// </summary>
public sealed class SaleConsumptionService(
    IMenuItemRepository menuItems,
    IInventoryMovementRepository movements,
    InventoryLedger ledger,
    ICurrentUser currentUser)
{
    /// <summary>Postea los <c>SALE</c> del pedido (idempotente). El pedido llega con sus ítems cargados.</summary>
    public async Task PostForOrderAsync(Order order, CancellationToken ct = default)
    {
        var requiredByIngredient = new Dictionary<int, decimal>();

        foreach (var item in order.Items)
        {
            var menuItem = await menuItems.GetAsync(item.MenuItemId, ct)
                ?? throw new NotFoundException("plato", item.MenuItemId);

            foreach (var line in menuItem.Recipe)
            {
                requiredByIngredient[line.IngredientId] =
                    requiredByIngredient.GetValueOrDefault(line.IngredientId)
                    + line.QuantityRequired * item.Quantity;
            }
        }

        foreach (var (ingredientId, rawQuantity) in requiredByIngredient)
        {
            var quantity = Money.Round(rawQuantity); // cantidades de inventario: decimal(10,2)
            if (quantity <= 0m)
            {
                continue;
            }

            await ledger.PostAsync(
                new LedgerEntry(
                    order.BranchId,
                    ingredientId,
                    MovementType.Sale,
                    -quantity,
                    MovementReference.To(MovementReferenceTypes.Order, order.Id),
                    currentUser.EmployeeId),
                ct);
        }
    }

    /// <summary>
    /// Revierte el consumo de un pedido ya contabilizado: por cada <c>SALE</c> con
    /// referencia <c>ORDER/orderId</c> postea un <c>ADJUSTMENT</c> de signo opuesto,
    /// con la misma referencia (idempotente ante reintentos de cancelación).
    /// </summary>
    public async Task ReverseForOrderAsync(int orderId, CancellationToken ct = default)
    {
        var posted = await movements.ListByReferenceAsync(MovementReferenceTypes.Order, orderId, ct);

        foreach (var sale in posted.Where(m => m.MovementType == MovementType.Sale))
        {
            await ledger.PostAsync(
                new LedgerEntry(
                    sale.BranchId,
                    sale.IngredientId,
                    MovementType.Adjustment,
                    -sale.Quantity, // SALE es negativo ⇒ la reversa devuelve stock
                    MovementReference.To(MovementReferenceTypes.Order, orderId),
                    currentUser.EmployeeId),
                ct);
        }
    }
}
