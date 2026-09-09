using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Menu;

namespace RestoManager.Business.Application.Menu.Categories;

public sealed record CategoryDto(int Id, string Name, string Description);

public sealed record SaveCategoryCommand(int? Id, string Name, string Description);

public sealed class SaveCategoryValidator : AbstractValidator<SaveCategoryCommand>
{
    public SaveCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public sealed class SaveCategoryHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IValidator<SaveCategoryCommand> validator)
{
    public async Task<int> HandleAsync(SaveCategoryCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        Category entity;
        if (command.Id is { } id)
        {
            entity = await categories.GetAsync(id, ct) ?? throw new NotFoundException("categoría", id);
            entity.Update(command.Name, command.Description);
        }
        else
        {
            entity = new Category(command.Name, command.Description);
            categories.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed record ListCategoriesQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListCategoriesHandler(ICategoryRepository categories)
{
    public async Task<PagedResult<CategoryDto>> HandleAsync(ListCategoriesQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await categories.ListAsync(query.Search, page.Skip, page.Take, ct);
        var total = await categories.CountAsync(query.Search, ct);
        return new PagedResult<CategoryDto>(
            items.Select(c => new CategoryDto(c.Id, c.Name, c.Description)).ToList(), query.Page, page.Take, total);
    }
}

public sealed class GetCategoryHandler(ICategoryRepository categories)
{
    public async Task<CategoryDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var c = await categories.GetAsync(id, ct) ?? throw new NotFoundException("categoría", id);
        return new CategoryDto(c.Id, c.Name, c.Description);
    }
}
