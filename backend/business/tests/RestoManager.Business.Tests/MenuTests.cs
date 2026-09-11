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
            item.SetRecipe([(10, 1m, true), (10, 2m, true)]));
        Assert.Equal("menu.recipe_duplicate_ingredient", ex.Code);
    }

    [Fact]
    public void SetRecipe_rejects_non_positive_quantity()
    {
        var item = NewItem();
        Assert.Throws<DomainRuleException>(() => item.SetRecipe([(10, 0m, true)]));
        Assert.Throws<DomainRuleException>(() => item.SetRecipe([(10, -1m, true)]));
    }

    [Fact]
    public void SetRecipe_replaces_previous_lines()
    {
        var item = NewItem();
        item.SetRecipe([(10, 1m, true), (11, 2m, true)]);
        item.SetRecipe([(12, 3m, true)]);

        Assert.Single(item.Recipe);
        Assert.Equal(12, item.Recipe[0].IngredientId);
        Assert.Equal(3m, item.Recipe[0].QuantityRequired);
    }

    [Fact]
    public void SetRecipe_carries_the_public_visibility_flag()
    {
        var item = NewItem();
        item.SetRecipe([(10, 1m, true), (11, 2m, false)]);

        Assert.True(item.Recipe.Single(r => r.IngredientId == 10).IsPublic);
        Assert.False(item.Recipe.Single(r => r.IngredientId == 11).IsPublic);
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

    [Fact]
    public void SetImage_then_ClearImage()
    {
        var item = NewItem();
        Assert.Null(item.ImageKey);

        item.SetImage("  abc123.webp  ");
        Assert.Equal("abc123.webp", item.ImageKey);

        item.ClearImage();
        Assert.Null(item.ImageKey);
    }

    [Fact]
    public void SetImage_rejects_blank_key()
        => Assert.Throws<ArgumentException>(() => NewItem().SetImage("   "));

    [Fact]
    public void Delete_sets_DeletedAt_and_IsDeleted()
    {
        var item = NewItem();
        Assert.False(item.IsDeleted);

        var now = new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);
        item.Delete(now);

        Assert.True(item.IsDeleted);
        Assert.Equal(now, item.DeletedAt);
    }

    [Fact]
    public void Delete_twice_is_rejected()
    {
        var item = NewItem();
        item.Delete(DateTime.UtcNow);

        var ex = Assert.Throws<DomainRuleException>(() => item.Delete(DateTime.UtcNow));
        Assert.Equal("menu.item_already_deleted", ex.Code);
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
        item.SetRecipe([(10, 0.25m, true), (11, 0.50m, true)]);

        var handler = new GetMenuItemCostHandler(
            new FakeMenuItems(item),
            new FakeIngredients(new Dictionary<int, decimal> { [10] = 2m, [11] = 4m }));

        var cost = await handler.HandleAsync(1);

        // 0.25 * 2 + 0.50 * 4 = 2.50
        Assert.Equal(2.50m, cost.Cost);
        Assert.Equal(2, cost.Lines.Count);
    }
}

public class DeleteMenuItemHandlerTests
{
    private sealed class FakeMenuItems(MenuItem? item) : IMenuItemRepository
    {
        public Task<MenuItem?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(item);
        public Task<bool> ExistsAsync(int id, CancellationToken ct = default) => Task.FromResult(item is not null);
        public Task<IReadOnlyList<MenuItem>> ListAsync(int? categoryId, string? search, int skip, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MenuItem>>(item is null ? [] : [item]);
        public Task<int> CountAsync(int? categoryId, string? search, CancellationToken ct = default) => Task.FromResult(item is null ? 0 : 1);
        public void Add(MenuItem menuItem) { }
    }

    private static readonly DateTime Now = new(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Delete_marks_the_item_without_touching_its_recipe()
    {
        var item = new MenuItem(1, "Pizza", "", 12m, true);
        item.SetRecipe([(10, 1m, true)]);
        var handler = new DeleteMenuItemHandler(new FakeMenuItems(item), new FixedClock(Now), new FakeUnitOfWork());

        await handler.HandleAsync(1);

        Assert.True(item.IsDeleted);
        Assert.Equal(Now, item.DeletedAt);
        // La receta sigue en el agregado; el borrado no toca los ingredientes referenciados (issue #47).
        Assert.Single(item.Recipe);
    }

    [Fact]
    public async Task Delete_missing_item_throws_not_found()
    {
        var handler = new DeleteMenuItemHandler(new FakeMenuItems(null), new FixedClock(Now), new FakeUnitOfWork());
        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(999));
    }
}
