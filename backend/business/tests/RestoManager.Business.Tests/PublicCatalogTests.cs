using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Menu.Items;
using RestoManager.Business.Application.Organization.Branches;
using RestoManager.Business.Application.PublicCatalog;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Tests;

// ---- Fakes compartidos por los tests de la carta pública (Fase 11) ----

internal static class Ids
{
    public static T WithId<T>(this T entity, int id)
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}

internal sealed class FakeBranches(params Branch[] branches) : IBranchRepository
{
    public Task<Branch?> GetAsync(int id, CancellationToken ct = default)
        => Task.FromResult(branches.FirstOrDefault(b => b.Id == id));
    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => Task.FromResult(branches.Any(b => b.Id == id));
    public Task<Branch?> GetByPublicSlugAsync(string slug, CancellationToken ct = default)
        => Task.FromResult(branches.FirstOrDefault(b => b.PublicSlug == slug));
    public Task<IReadOnlyList<Branch>> ListAsync(int? restaurantId, int skip, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Branch>>(branches);
    public Task<int> CountAsync(int? restaurantId, CancellationToken ct = default)
        => Task.FromResult(branches.Length);
    public void Add(Branch branch) { }
}

internal sealed class FakeRestaurants(params Restaurant[] restaurants) : IRestaurantRepository
{
    public Task<Restaurant?> GetAsync(int id, CancellationToken ct = default)
        => Task.FromResult(restaurants.FirstOrDefault(r => r.Id == id));
    public Task<IReadOnlyList<Restaurant>> ListAsync(int skip, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Restaurant>>(restaurants);
    public Task<int> CountAsync(CancellationToken ct = default) => Task.FromResult(restaurants.Length);
    public void Add(Restaurant restaurant) { }
}

internal sealed class FakeCategories(params Category[] categories) : ICategoryRepository
{
    public Task<Category?> GetAsync(int id, CancellationToken ct = default)
        => Task.FromResult(categories.FirstOrDefault(c => c.Id == id));
    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => Task.FromResult(categories.Any(c => c.Id == id));
    public Task<IReadOnlyList<Category>> ListAsync(string? search, int skip, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Category>>(categories);
    public Task<int> CountAsync(string? search, CancellationToken ct = default) => Task.FromResult(categories.Length);
    public void Add(Category category) { }
}

internal sealed class FakeMenuItemsRepo(params MenuItem[] items) : IMenuItemRepository
{
    public Task<MenuItem?> GetAsync(int id, CancellationToken ct = default)
        => Task.FromResult(items.FirstOrDefault(m => m.Id == id));
    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => Task.FromResult(items.Any(m => m.Id == id));
    public Task<IReadOnlyList<MenuItem>> ListAsync(int? categoryId, string? search, int skip, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<MenuItem>>(items);
    public Task<int> CountAsync(int? categoryId, string? search, CancellationToken ct = default) => Task.FromResult(items.Length);
    public void Add(MenuItem menuItem) { }
}

internal sealed class FakeIngredientsRepo(params Ingredient[] ingredients) : IIngredientRepository
{
    public Task<Ingredient?> GetAsync(int id, CancellationToken ct = default)
        => Task.FromResult(ingredients.FirstOrDefault(i => i.Id == id));
    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => Task.FromResult(ingredients.Any(i => i.Id == id));
    public Task<IReadOnlyList<Ingredient>> ListAsync(string? search, int skip, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Ingredient>>(ingredients);
    public Task<int> CountAsync(string? search, CancellationToken ct = default) => Task.FromResult(ingredients.Length);
    public void Add(Ingredient ingredient) { }
}

internal sealed class FakeAvailability(params MenuItemBranchAvailability[] rows) : IMenuItemAvailabilityRepository
{
    public Task<MenuItemBranchAvailability?> GetAsync(int branchId, int menuItemId, CancellationToken ct = default)
        => Task.FromResult(rows.FirstOrDefault(r => r.BranchId == branchId && r.MenuItemId == menuItemId));
    public Task<IReadOnlyList<MenuItemBranchAvailability>> ListByBranchAsync(int branchId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<MenuItemBranchAvailability>>(rows.Where(r => r.BranchId == branchId).ToList());
    public void Add(MenuItemBranchAvailability row) { }
}

internal sealed class FakeImageStorage : IImageStorage
{
    public List<string> Saved { get; } = [];
    public List<string> Deleted { get; } = [];
    public int NextKey { get; set; } = 1;

    public Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        var ext = contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            _ => "webp",
        };
        var key = $"img-{NextKey++}.{ext}";
        Saved.Add(key);
        return Task.FromResult(key);
    }

    public void Delete(string key) => Deleted.Add(key);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        => action(cancellationToken);
}

// ---- GetPublicCatalogHandler ----

public class GetPublicCatalogHandlerTests
{
    private static MenuItem Item(int id, int categoryId, string name, bool available, string? imageKey = null, (int id, decimal qty)[]? recipe = null)
    {
        var m = new MenuItem(categoryId, name, $"desc {name}", 10m, available).WithId(id);
        if (imageKey is not null)
        {
            m.SetImage(imageKey);
        }
        if (recipe is not null)
        {
            m.SetRecipe(recipe.Select(r => (r.id, r.qty)));
        }
        return m;
    }

    private static GetPublicCatalogHandler Build(
        Branch branch, MenuItem[] items, MenuItemBranchAvailability[] overrides, Ingredient[] ingredients, Category[] categories)
        => new(
            new FakeBranches(branch),
            new FakeRestaurants(new Restaurant("Demo", "Calle 1", "", "", "30-1").WithId(branch.RestaurantId)),
            new FakeCategories(categories),
            new FakeMenuItemsRepo(items),
            new FakeIngredientsRepo(ingredients),
            new FakeAvailability(overrides));

    private static Branch BranchWithSlug(string slug)
    {
        var b = new Branch(1, "Centro", "Av. Central 100", "", "", new TimeOnly(8, 0), new TimeOnly(23, 0)).WithId(7);
        b.SetPublicSlug(slug);
        return b;
    }

    [Fact]
    public async Task Unknown_slug_is_not_found()
    {
        var handler = Build(BranchWithSlug("centro"), [], [], [], []);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync("otro"));
    }

    [Fact]
    public async Task Slug_lookup_is_case_insensitive_and_trimmed()
    {
        var cat = new Category("Entradas", "").WithId(1);
        var handler = Build(BranchWithSlug("centro"), [Item(1, 1, "Bruschetta", available: true)], [], [], [cat]);

        var catalog = await handler.HandleAsync("  CENTRO ");

        Assert.Equal("Demo", catalog.Restaurant.Name);
        Assert.Equal("Centro", catalog.Branch.Name);
        Assert.Single(catalog.Items);
    }

    [Fact]
    public async Task Excludes_items_not_available_by_default()
    {
        var cat = new Category("Principales", "").WithId(1);
        var items = new[]
        {
            Item(1, 1, "Visible", available: true),
            Item(2, 1, "Oculto", available: false),
        };
        var handler = Build(BranchWithSlug("centro"), items, [], [], [cat]);

        var catalog = await handler.HandleAsync("centro");

        Assert.Equal(["Visible"], catalog.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Branch_override_wins_over_default_both_ways()
    {
        var cat = new Category("Principales", "").WithId(1);
        var items = new[]
        {
            Item(1, 1, "DefaultOn-HiddenHere", available: true),
            Item(2, 1, "DefaultOff-ShownHere", available: false),
        };
        var overrides = new[]
        {
            new MenuItemBranchAvailability(7, 1, isAvailable: false),
            new MenuItemBranchAvailability(7, 2, isAvailable: true),
        };
        var handler = Build(BranchWithSlug("centro"), items, overrides, [], [cat]);

        var catalog = await handler.HandleAsync("centro");

        Assert.Equal(["DefaultOff-ShownHere"], catalog.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Override_for_other_branch_is_ignored()
    {
        var cat = new Category("Principales", "").WithId(1);
        var items = new[] { Item(1, 1, "Plato", available: true) };
        var overrides = new[] { new MenuItemBranchAvailability(99, 1, isAvailable: false) };
        var handler = Build(BranchWithSlug("centro"), items, overrides, [], [cat]);

        var catalog = await handler.HandleAsync("centro");

        Assert.Single(catalog.Items);
    }

    [Fact]
    public async Task Ingredients_are_names_only_sorted_without_units_or_quantities()
    {
        var cat = new Category("Principales", "").WithId(1);
        var ingredients = new[]
        {
            new Ingredient("Tomate", "kg", 1m).WithId(10),
            new Ingredient("Albahaca", "atado", 1m).WithId(11),
            new Ingredient("Mozzarella", "kg", 1m).WithId(12),
        };
        var item = Item(1, 1, "Caprese", available: true, recipe: [(10, 0.2m), (11, 0.05m), (12, 0.15m)]);
        var handler = Build(BranchWithSlug("centro"), [item], [], ingredients, [cat]);

        var catalog = await handler.HandleAsync("centro");

        Assert.Equal(["Albahaca", "Mozzarella", "Tomate"], catalog.Items.Single().Ingredients);
    }

    [Fact]
    public async Task Item_carries_category_name_and_image_url()
    {
        var cat = new Category("Entradas", "").WithId(3);
        var item = Item(1, 3, "Rabas", available: true, imageKey: "rabas.webp");
        var handler = Build(BranchWithSlug("centro"), [item], [], [], [cat]);

        var dto = (await handler.HandleAsync("centro")).Items.Single();

        Assert.Equal("Entradas", dto.CategoryName);
        Assert.Equal("/media/menu/rabas.webp", dto.ImageUrl);
    }

    [Fact]
    public async Task Only_categories_with_visible_items_are_returned()
    {
        var entradas = new Category("Entradas", "").WithId(1);
        var postres = new Category("Postres", "").WithId(2);
        var items = new[]
        {
            Item(1, 1, "Empanada", available: true),
            Item(2, 2, "Flan", available: false),
        };
        var handler = Build(BranchWithSlug("centro"), items, [], [], [entradas, postres]);

        var catalog = await handler.HandleAsync("centro");

        Assert.Equal(["Entradas"], catalog.Categories.Select(c => c.Name));
    }
}

// ---- SetBranchPublicSlugHandler ----

public class SetBranchPublicSlugHandlerTests
{
    private static Branch NewBranch(int id, string? slug = null)
    {
        var b = new Branch(1, $"Sucursal {id}", "Calle 1", "", "", new TimeOnly(8, 0), new TimeOnly(23, 0)).WithId(id);
        if (slug is not null)
        {
            b.SetPublicSlug(slug);
        }
        return b;
    }

    [Fact]
    public async Task Sets_slug_and_persists()
    {
        var branch = NewBranch(1);
        var uow = new FakeUnitOfWork();
        var handler = new SetBranchPublicSlugHandler(new FakeBranches(branch), uow);

        await handler.HandleAsync(new SetBranchPublicSlugCommand(1, " Centro "));

        Assert.Equal("centro", branch.PublicSlug);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Clearing_slug_is_allowed()
    {
        var branch = NewBranch(1, "centro");
        var handler = new SetBranchPublicSlugHandler(new FakeBranches(branch), new FakeUnitOfWork());

        await handler.HandleAsync(new SetBranchPublicSlugCommand(1, null));

        Assert.Null(branch.PublicSlug);
    }

    [Fact]
    public async Task Slug_taken_by_another_branch_is_rejected()
    {
        var mine = NewBranch(1);
        var other = NewBranch(2, "centro");
        var handler = new SetBranchPublicSlugHandler(new FakeBranches(mine, other), new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<DomainRuleException>(
            () => handler.HandleAsync(new SetBranchPublicSlugCommand(1, "centro")));
        Assert.Equal("branch.slug_taken", ex.Code);
    }

    [Fact]
    public async Task Re_setting_own_slug_is_not_a_conflict()
    {
        var branch = NewBranch(1, "centro");
        var handler = new SetBranchPublicSlugHandler(new FakeBranches(branch), new FakeUnitOfWork());

        await handler.HandleAsync(new SetBranchPublicSlugCommand(1, "centro"));

        Assert.Equal("centro", branch.PublicSlug);
    }

    [Fact]
    public async Task Unknown_branch_is_not_found()
    {
        var handler = new SetBranchPublicSlugHandler(new FakeBranches(), new FakeUnitOfWork());
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(new SetBranchPublicSlugCommand(404, "centro")));
    }
}

// ---- SetMenuItemImageHandler / ClearMenuItemImageHandler ----

public class MenuItemImageHandlerTests
{
    private static MenuItem NewItem(int id, string? imageKey = null)
    {
        var m = new MenuItem(1, "Pizza", "", 12m, true);
        typeof(MenuItem).GetProperty("Id")!.SetValue(m, id);
        if (imageKey is not null)
        {
            m.SetImage(imageKey);
        }
        return m;
    }

    private static Stream SomeBytes() => new MemoryStream([1, 2, 3, 4]);

    [Fact]
    public async Task Rejects_unsupported_content_type()
    {
        var handler = new SetMenuItemImageHandler(
            new FakeMenuItemsRepo(NewItem(1)), new FakeImageStorage(), new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<InvalidInputException>(() => handler.HandleAsync(
            new SetMenuItemImageCommand(1, SomeBytes(), "application/pdf", 100)));
        Assert.Equal("menu.image_type", ex.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2 * 1024 * 1024 + 1)]
    public async Task Rejects_out_of_range_size(long length)
    {
        var handler = new SetMenuItemImageHandler(
            new FakeMenuItemsRepo(NewItem(1)), new FakeImageStorage(), new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<InvalidInputException>(() => handler.HandleAsync(
            new SetMenuItemImageCommand(1, SomeBytes(), "image/png", length)));
        Assert.Equal("menu.image_size", ex.Code);
    }

    [Fact]
    public async Task Saves_sets_key_and_returns_public_url()
    {
        var item = NewItem(1);
        var storage = new FakeImageStorage();
        var handler = new SetMenuItemImageHandler(new FakeMenuItemsRepo(item), storage, new FakeUnitOfWork());

        var result = await handler.HandleAsync(
            new SetMenuItemImageCommand(1, SomeBytes(), "image/webp", 1234));

        Assert.Equal("img-1.webp", item.ImageKey);
        Assert.Equal("/media/menu/img-1.webp", result.ImageUrl);
        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task Replacing_an_image_deletes_the_previous_one()
    {
        var item = NewItem(1, "old.webp");
        var storage = new FakeImageStorage();
        var handler = new SetMenuItemImageHandler(new FakeMenuItemsRepo(item), storage, new FakeUnitOfWork());

        await handler.HandleAsync(new SetMenuItemImageCommand(1, SomeBytes(), "image/png", 999));

        Assert.Equal(["old.webp"], storage.Deleted);
        Assert.Equal("img-1.png", item.ImageKey);
    }

    [Fact]
    public async Task Unknown_item_is_not_found()
    {
        var handler = new SetMenuItemImageHandler(
            new FakeMenuItemsRepo(), new FakeImageStorage(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(
            new SetMenuItemImageCommand(1, SomeBytes(), "image/png", 10)));
    }

    [Fact]
    public async Task Clear_removes_key_and_file()
    {
        var item = NewItem(1, "keep-then-drop.webp");
        var storage = new FakeImageStorage();
        var handler = new ClearMenuItemImageHandler(new FakeMenuItemsRepo(item), storage, new FakeUnitOfWork());

        await handler.HandleAsync(1);

        Assert.Null(item.ImageKey);
        Assert.Equal(["keep-then-drop.webp"], storage.Deleted);
    }

    [Fact]
    public async Task Clear_on_item_without_image_is_a_no_op()
    {
        var item = NewItem(1);
        var storage = new FakeImageStorage();
        var uow = new FakeUnitOfWork();
        var handler = new ClearMenuItemImageHandler(new FakeMenuItemsRepo(item), storage, uow);

        await handler.HandleAsync(1);

        Assert.Empty(storage.Deleted);
        Assert.Equal(0, uow.SaveCount);
    }
}
