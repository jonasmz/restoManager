using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;

namespace RestoManager.Business.Application.Inventory.Ingredients;

public sealed record IngredientDto(int Id, string Name, string Unit, decimal UnitPrice, decimal ReorderPoint);

// ---- Crear ----
public sealed record CreateIngredientCommand(string Name, string Unit, decimal UnitPrice, decimal ReorderPoint = 0m);

public sealed class CreateIngredientValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateIngredientHandler(
    IIngredientRepository ingredients,
    IUnitOfWork unitOfWork,
    IValidator<CreateIngredientCommand> validator)
{
    public async Task<int> HandleAsync(CreateIngredientCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var ingredient = new Ingredient(command.Name, command.Unit, command.UnitPrice, command.ReorderPoint);
        ingredients.Add(ingredient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ingredient.Id;
    }
}

// ---- Actualizar ----
public sealed record UpdateIngredientCommand(int Id, string Name, string Unit, decimal UnitPrice, decimal ReorderPoint = 0m);

public sealed class UpdateIngredientValidator : AbstractValidator<UpdateIngredientCommand>
{
    public UpdateIngredientValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateIngredientHandler(
    IIngredientRepository ingredients,
    IUnitOfWork unitOfWork,
    IValidator<UpdateIngredientCommand> validator)
{
    public async Task HandleAsync(UpdateIngredientCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var ingredient = await ingredients.GetAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("ingrediente", command.Id);

        ingredient.Rename(command.Name);
        ingredient.SetUnit(command.Unit);
        ingredient.SetUnitPrice(command.UnitPrice);
        ingredient.SetReorderPoint(command.ReorderPoint);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// ---- Consultas ----
public sealed record ListIngredientsQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListIngredientsHandler(IIngredientRepository ingredients)
{
    public async Task<PagedResult<IngredientDto>> HandleAsync(ListIngredientsQuery query, CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await ingredients.ListAsync(query.Search, page.Skip, page.Take, cancellationToken);
        var total = await ingredients.CountAsync(query.Search, cancellationToken);
        return new PagedResult<IngredientDto>(
            items.Select(i => new IngredientDto(i.Id, i.Name, i.Unit, i.UnitPrice, i.ReorderPoint)).ToList(),
            query.Page, page.Take, total);
    }
}

public sealed class GetIngredientHandler(IIngredientRepository ingredients)
{
    public async Task<IngredientDto> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var i = await ingredients.GetAsync(id, cancellationToken) ?? throw new NotFoundException("ingrediente", id);
        return new IngredientDto(i.Id, i.Name, i.Unit, i.UnitPrice, i.ReorderPoint);
    }
}
