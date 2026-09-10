using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Purchasing;

namespace RestoManager.Business.Tests;

internal sealed class FixedClock(DateTime now) : IClock
{
    public DateTime UtcNow { get; } = now;
}

internal sealed class FakeBalances : IBranchInventoryRepository
{
    public List<BranchInventory> Rows { get; } = [];

    public Task<BranchInventory?> GetAsync(int branchId, int ingredientId, CancellationToken ct = default)
        => Task.FromResult(Rows.FirstOrDefault(r => r.BranchId == branchId && r.IngredientId == ingredientId));

    public Task<IReadOnlyList<BranchInventory>> ListByBranchAsync(int branchId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<BranchInventory>>(Rows.Where(r => r.BranchId == branchId).ToList());

    public void Add(BranchInventory balance) => Rows.Add(balance);
}

internal sealed class FakeMovements : IInventoryMovementRepository
{
    public List<InventoryMovement> Rows { get; } = [];

    public void Add(InventoryMovement movement) => Rows.Add(movement);

    public Task<bool> ExistsForReferenceAsync(
        string referenceType, int referenceId, MovementType movementType, int ingredientId, CancellationToken ct = default)
        => Task.FromResult(Rows.Any(m =>
            m.ReferenceType == referenceType && m.ReferenceId == referenceId &&
            m.MovementType == movementType && m.IngredientId == ingredientId));

    public Task<IReadOnlyList<InventoryMovement>> ListAsync(
        int branchId, int? ingredientId, DateTime? from, DateTime? to, MovementType? movementType, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<InventoryMovement>>(Rows);

    public Task<IReadOnlyList<InventoryMovement>> ListByReferenceAsync(
        string referenceType, int referenceId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<InventoryMovement>>(
            Rows.Where(m => m.ReferenceType == referenceType && m.ReferenceId == referenceId).ToList());
}

public class InventoryLedgerTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Unspecified);

    private static (InventoryLedger ledger, FakeBalances balances, FakeMovements movements) Build()
    {
        var balances = new FakeBalances();
        var movements = new FakeMovements();
        return (new InventoryLedger(balances, movements, new FixedClock(Now)), balances, movements);
    }

    [Fact]
    public async Task Purchase_creates_movement_and_raises_balance()
    {
        var (ledger, balances, movements) = Build();

        var posted = await ledger.PostAsync(new LedgerEntry(
            1, 10, MovementType.Purchase, 25m, MovementReference.To("PURCHASE_ORDER", 3), 7));

        Assert.True(posted);
        Assert.Single(movements.Rows);
        Assert.Equal(25m, movements.Rows[0].Quantity);
        Assert.Equal(25m, balances.Rows.Single().StockQuantity);
    }

    [Fact]
    public async Task Wrong_sign_is_rejected()
    {
        var (ledger, _, _) = Build();

        var ex = await Assert.ThrowsAsync<DomainRuleException>(() => ledger.PostAsync(new LedgerEntry(
            1, 10, MovementType.Purchase, -5m, MovementReference.None, null)));

        Assert.Equal("inventory.wrong_sign", ex.Code);
    }

    [Fact]
    public async Task Sale_beyond_stock_is_rejected()
    {
        var (ledger, _, _) = Build();
        await ledger.PostAsync(new LedgerEntry(1, 10, MovementType.Purchase, 5m, MovementReference.None, null));

        var ex = await Assert.ThrowsAsync<DomainRuleException>(() => ledger.PostAsync(new LedgerEntry(
            1, 10, MovementType.Sale, -8m, MovementReference.None, null)));

        Assert.Equal("inventory.insufficient_stock", ex.Code);
    }

    [Fact]
    public async Task Same_reference_is_idempotent()
    {
        var (ledger, balances, movements) = Build();
        var entry = new LedgerEntry(1, 10, MovementType.Purchase, 12m, MovementReference.To("PURCHASE_ORDER", 99), 7);

        Assert.True(await ledger.PostAsync(entry));
        Assert.False(await ledger.PostAsync(entry));

        Assert.Single(movements.Rows);
        Assert.Equal(12m, balances.Rows.Single().StockQuantity);
    }

    [Fact]
    public async Task Adjustment_can_be_negative_but_not_below_zero()
    {
        var (ledger, balances, _) = Build();
        await ledger.PostAsync(new LedgerEntry(1, 10, MovementType.Purchase, 10m, MovementReference.None, null));

        await ledger.PostAsync(new LedgerEntry(1, 10, MovementType.Adjustment, -4m,
            new MovementReference("MANUAL_ADJUSTMENT", null), null));
        Assert.Equal(6m, balances.Rows.Single().StockQuantity);

        await Assert.ThrowsAsync<DomainRuleException>(() => ledger.PostAsync(new LedgerEntry(
            1, 10, MovementType.Adjustment, -20m, new MovementReference("MANUAL_ADJUSTMENT", null), null)));
    }
}

public class PurchaseOrderTests
{
    private static readonly (int, decimal, decimal)[] Lines = [(1, 3m, 10m), (2, 2m, 5m)];

    [Fact]
    public void Draft_computes_total_and_starts_in_draft()
    {
        var po = PurchaseOrder.Draft(1, 1, new DateOnly(2026, 9, 9), Lines);

        Assert.Equal(PurchaseOrderStatus.Draft, po.Status);
        Assert.Equal(40m, po.TotalAmount);
        Assert.Equal(2, po.Items.Count);
    }

    [Fact]
    public void Transitions_follow_the_catalog()
    {
        var po = PurchaseOrder.Draft(1, 1, new DateOnly(2026, 9, 9), Lines);

        Assert.Throws<DomainRuleException>(po.MarkReceived); // no se puede recibir un DRAFT
        po.Send();
        Assert.Equal(PurchaseOrderStatus.Sent, po.Status);
        po.MarkReceived();
        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        Assert.Throws<DomainRuleException>(po.Cancel); // no se cancela una recibida
    }

    [Fact]
    public void Empty_order_is_rejected()
        => Assert.Throws<DomainRuleException>(() =>
            PurchaseOrder.Draft(1, 1, new DateOnly(2026, 9, 9), []));
}

public class IngredientReorderPointTests
{
    [Fact]
    public void Defaults_to_zero_and_can_be_set()
    {
        var i = new Ingredient("Harina", "kg", 0.90m);
        Assert.Equal(0m, i.ReorderPoint);

        i.SetReorderPoint(15.5m);
        Assert.Equal(15.5m, i.ReorderPoint);
    }

    [Fact]
    public void Constructor_takes_reorder_point()
        => Assert.Equal(20m, new Ingredient("Tomate", "kg", 1.40m, reorderPoint: 20m).ReorderPoint);

    [Fact]
    public void Negative_reorder_point_is_rejected()
    {
        var i = new Ingredient("Sal", "kg", 0.10m);
        Assert.Throws<ArgumentOutOfRangeException>(() => i.SetReorderPoint(-1m));
    }
}

public class MovementTypeTests
{
    [Theory]
    [InlineData(MovementType.Purchase, "PURCHASE", 1)]
    [InlineData(MovementType.Sale, "SALE", -1)]
    [InlineData(MovementType.Waste, "WASTE", -1)]
    [InlineData(MovementType.Adjustment, "ADJUSTMENT", 0)]
    public void Db_value_and_sign_are_consistent(MovementType type, string db, int sign)
    {
        Assert.Equal(db, type.ToDbValue());
        Assert.Equal(type, MovementTypeExtensions.FromDbValue(db));
        Assert.Equal(sign, type.ExpectedSign());
    }
}
