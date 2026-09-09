using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Application.Sales.Consumption;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Tests;

public class SaleConsumptionServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 21, 0, 0, DateTimeKind.Unspecified);
    private const int BranchId = 1;

    private sealed class FakeMenu(Dictionary<int, MenuItem> byId) : IMenuItemRepository
    {
        public Task<MenuItem?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(byId.GetValueOrDefault(id));
        public Task<bool> ExistsAsync(int id, CancellationToken ct = default) => Task.FromResult(byId.ContainsKey(id));
        public Task<IReadOnlyList<MenuItem>> ListAsync(int? c, string? s, int skip, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MenuItem>>(byId.Values.ToList());
        public Task<int> CountAsync(int? c, string? s, CancellationToken ct = default) => Task.FromResult(byId.Count);
        public void Add(MenuItem menuItem) { }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int UserId => 1;
        public string Email => "e@resto.local";
        public IReadOnlyList<string> Roles => ["WAITER"];
        public int EmployeeId => 5;
        public IReadOnlyList<int> BranchIds => [BranchId];
        public bool IsInRole(string role) => Roles.Contains(role);
        public bool CanOperateInBranch(int branchId) => true;
    }

    private static MenuItem MenuItemWithRecipe(int id, params (int ingredientId, decimal qty)[] recipe)
    {
        var mi = new MenuItem(1, $"item{id}", "", 10m, true);
        mi.SetRecipe(recipe);
        typeof(MenuItem).GetProperty(nameof(MenuItem.Id))!.SetValue(mi, id);
        return mi;
    }

    private static Order OrderWithItems(int id, params (int menuItemId, int qty)[] items)
    {
        var order = Order.Create(OrderChannel.Barra, BranchId, 5, Now, null, null, null);
        foreach (var (menuItemId, qty) in items)
        {
            order.AddItem(menuItemId, qty, unitPrice: 10m, notes: null);
        }
        typeof(Order).GetProperty(nameof(Order.Id))!.SetValue(order, id);
        return order;
    }

    private static (SaleConsumptionService svc, FakeBalances balances, FakeMovements movements) Build(
        Dictionary<int, MenuItem> menu, params int[] stockedIngredients)
    {
        var balances = new FakeBalances();
        foreach (var ingredientId in stockedIngredients)
        {
            var b = BranchInventory.Start(BranchId, ingredientId);
            b.Apply(100m);
            balances.Add(b);
        }
        var movements = new FakeMovements();
        var ledger = new InventoryLedger(balances, movements, new FixedClock(Now));
        return (new SaleConsumptionService(new FakeMenu(menu), movements, ledger, new FakeCurrentUser()), balances, movements);
    }

    [Fact]
    public async Task Consumption_groups_required_quantity_by_ingredient()
    {
        var menu = new Dictionary<int, MenuItem>
        {
            [1] = MenuItemWithRecipe(1, (10, 0.15m)),
            [2] = MenuItemWithRecipe(2, (10, 0.10m), (11, 0.20m)),
        };
        var (svc, balances, movements) = Build(menu, 10, 11);

        await svc.PostForOrderAsync(OrderWithItems(42, (1, 2), (2, 1)));

        var byIngredient = movements.Rows.ToDictionary(m => m.IngredientId);
        Assert.Equal(MovementType.Sale, byIngredient[10].MovementType);
        Assert.Equal(-0.40m, byIngredient[10].Quantity);   // 0.15*2 + 0.10*1
        Assert.Equal(-0.20m, byIngredient[11].Quantity);
        Assert.All(movements.Rows, m => Assert.Equal("ORDER", m.ReferenceType));
        Assert.All(movements.Rows, m => Assert.Equal(42, m.ReferenceId));

        Assert.Equal(100m - 0.40m, balances.Rows.Single(b => b.IngredientId == 10).StockQuantity);
        Assert.Equal(100m - 0.20m, balances.Rows.Single(b => b.IngredientId == 11).StockQuantity);
    }

    [Fact]
    public async Task Consumption_is_idempotent_per_order()
    {
        var menu = new Dictionary<int, MenuItem> { [1] = MenuItemWithRecipe(1, (10, 0.15m)) };
        var (svc, balances, movements) = Build(menu, 10);
        var order = OrderWithItems(42, (1, 2));

        await svc.PostForOrderAsync(order);
        await svc.PostForOrderAsync(order);

        Assert.Single(movements.Rows);
        Assert.Equal(100m - 0.30m, balances.Rows.Single().StockQuantity);
    }

    [Fact]
    public async Task Items_without_recipe_consume_nothing()
    {
        var menu = new Dictionary<int, MenuItem> { [1] = MenuItemWithRecipe(1) };
        var (svc, _, movements) = Build(menu);

        await svc.PostForOrderAsync(OrderWithItems(42, (1, 3)));

        Assert.Empty(movements.Rows);
    }

    [Fact]
    public async Task Reverse_posts_opposite_adjustments_and_restores_stock()
    {
        var menu = new Dictionary<int, MenuItem>
        {
            [1] = MenuItemWithRecipe(1, (10, 0.15m)),
            [2] = MenuItemWithRecipe(2, (11, 0.20m)),
        };
        var (svc, balances, movements) = Build(menu, 10, 11);
        var order = OrderWithItems(42, (1, 2), (2, 1));

        await svc.PostForOrderAsync(order);
        await svc.ReverseForOrderAsync(42);

        var adjustments = movements.Rows.Where(m => m.MovementType == MovementType.Adjustment).ToList();
        Assert.Equal(2, adjustments.Count);
        Assert.Equal(0.30m, adjustments.Single(m => m.IngredientId == 10).Quantity);
        Assert.Equal(0.20m, adjustments.Single(m => m.IngredientId == 11).Quantity);

        Assert.Equal(100m, balances.Rows.Single(b => b.IngredientId == 10).StockQuantity);
        Assert.Equal(100m, balances.Rows.Single(b => b.IngredientId == 11).StockQuantity);
    }

    [Fact]
    public async Task Reverse_is_idempotent()
    {
        var menu = new Dictionary<int, MenuItem> { [1] = MenuItemWithRecipe(1, (10, 0.15m)) };
        var (svc, balances, movements) = Build(menu, 10);
        var order = OrderWithItems(42, (1, 2));

        await svc.PostForOrderAsync(order);
        await svc.ReverseForOrderAsync(42);
        await svc.ReverseForOrderAsync(42);

        Assert.Single(movements.Rows.Where(m => m.MovementType == MovementType.Adjustment));
        Assert.Equal(100m, balances.Rows.Single().StockQuantity);
    }
}
