using RestoManager.Business.Application.Menu.Items;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Tax;

namespace RestoManager.Business.Tests;

public class MenuItemRecipeTests
{
    private static MenuItem NewItem() => new(1, "Pizza", "", 12m, true);

    [Fact]
    public void SetRecipe_rejects_duplicate_ingredient()
    {
        var item = NewItem();
        var ex = Assert.Throws<DomainRuleException>(() =>
            item.SetRecipe([(10, 1m), (10, 2m)]));
        Assert.Equal("menu.recipe_duplicate_ingredient", ex.Code);
    }

    [Fact]
    public void SetRecipe_rejects_non_positive_quantity()
    {
        var item = NewItem();
        Assert.Throws<DomainRuleException>(() => item.SetRecipe([(10, 0m)]));
        Assert.Throws<DomainRuleException>(() => item.SetRecipe([(10, -1m)]));
    }

    [Fact]
    public void SetRecipe_replaces_previous_lines()
    {
        var item = NewItem();
        item.SetRecipe([(10, 1m), (11, 2m)]);
        item.SetRecipe([(12, 3m)]);

        Assert.Single(item.Recipe);
        Assert.Equal(12, item.Recipe[0].IngredientId);
        Assert.Equal(3m, item.Recipe[0].QuantityRequired);
    }

    [Fact]
    public void SetTaxes_rejects_duplicate_rate()
    {
        var item = NewItem();
        var ex = Assert.Throws<DomainRuleException>(() => item.SetTaxes([5, 5]));
        Assert.Equal("menu.tax_duplicate", ex.Code);
    }

    [Fact]
    public void SetPrice_rejects_negative()
    {
        var ex = Assert.Throws<DomainRuleException>(() => NewItem().SetPrice(-0.01m));
        Assert.Equal("menu.invalid_price", ex.Code);
    }
}

public class KitchenStationTests
{
    [Fact]
    public void SetMenuItems_rejects_duplicate_item()
    {
        var station = new KitchenStation(1, "Caliente", "");
        var ex = Assert.Throws<DomainRuleException>(() => station.SetMenuItems([7, 7]));
        Assert.Equal("menu.station_duplicate_item", ex.Code);
    }

    [Fact]
    public void SetMenuItems_replaces_previous_assignment()
    {
        var station = new KitchenStation(1, "Caliente", "");
        station.SetMenuItems([1, 2, 3]);
        station.SetMenuItems([9]);

        Assert.Single(station.MenuItems);
        Assert.Equal(9, station.MenuItems[0].MenuItemId);
    }
}

public class TaxRateTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void Rate_outside_0_to_100_is_rejected(decimal rate)
    {
        var ex = Assert.Throws<DomainRuleException>(() => new TaxRate("X", rate));
        Assert.Equal("fiscal.invalid_rate", ex.Code);
    }

    [Fact]
    public void Update_trims_name_and_rejects_blank()
    {
        var rate = new TaxRate("  IVA  ", 21m);
        Assert.Equal("IVA", rate.Name);
        Assert.Throws<ArgumentException>(() => rate.Update("   ", 21m));
    }
}

public class MenuItemCostTests
{
    private sealed class FakeMenuItems(MenuItem item) : IMenuItemRepository
    {
        public Task<MenuItem?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult<MenuItem?>(item);
        public Task<bool> ExistsAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<MenuItem>> ListAsync(int? categoryId, string? search, int skip, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MenuItem>>([item]);
        public Task<int> CountAsync(int? categoryId, string? search, CancellationToken ct = default) => Task.FromResult(1);
        public void Add(MenuItem menuItem) { }
    }

    private sealed class FakeIngredients(Dictionary<int, decimal> unitPriceById) : IIngredientRepository
    {
        public Task<Ingredient?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(unitPriceById.TryGetValue(id, out var price)
                ? new Ingredient($"ing-{id}", "kg", price)
                : null);
        public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
            => Task.FromResult(unitPriceById.ContainsKey(id));
        public Task<IReadOnlyList<Ingredient>> ListAsync(string? search, int skip, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Ingredient>>([]);
        public Task<int> CountAsync(string? search, CancellationToken ct = default) => Task.FromResult(0);
        public void Add(Ingredient ingredient) { }
    }

    [Fact]
    public async Task Cost_is_sum_of_quantity_times_unit_price()
    {
        var item = new MenuItem(1, "Pizza", "", 12m, true);
        item.SetRecipe([(10, 0.25m), (11, 0.50m)]);

        var handler = new GetMenuItemCostHandler(
            new FakeMenuItems(item),
            new FakeIngredients(new Dictionary<int, decimal> { [10] = 2m, [11] = 4m }));

        var cost = await handler.HandleAsync(1);

        // 0.25 * 2 + 0.50 * 4 = 2.50
        Assert.Equal(2.50m, cost.Cost);
        Assert.Equal(2, cost.Lines.Count);
    }
}
