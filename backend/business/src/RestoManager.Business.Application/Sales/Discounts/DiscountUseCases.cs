using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Application.Sales.Discounts;

public sealed record DiscountDto(
    int Id, string Name, string Type, decimal Value, DateOnly StartDate, DateOnly EndDate);

// ---- Guardar (crea o actualiza) ----
public sealed record SaveDiscountCommand(
    int? Id, string Name, string Type, decimal Value, DateOnly StartDate, DateOnly EndDate);

public sealed class SaveDiscountValidator : AbstractValidator<SaveDiscountCommand>
{
    public SaveDiscountValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type)
            .Must(t => DiscountTypeExtensions.TryFromDbValue(t, out _))
            .WithMessage("Tipo inválido. Use PERCENTAGE o FIXED_AMOUNT.");
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public sealed class SaveDiscountHandler(
    IDiscountRepository discounts, IUnitOfWork unitOfWork, IValidator<SaveDiscountCommand> validator)
{
    public async Task<int> HandleAsync(SaveDiscountCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var type = DiscountTypeExtensions.FromDbValue(command.Type);

        Discount entity;
        if (command.Id is { } id)
        {
            entity = await discounts.GetAsync(id, ct) ?? throw new NotFoundException("descuento", id);
            entity.Update(command.Name, type, command.Value, command.StartDate, command.EndDate);
        }
        else
        {
            entity = new Discount(command.Name, type, command.Value, command.StartDate, command.EndDate);
            discounts.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

// ---- Consultas ----
public sealed record ListDiscountsQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListDiscountsHandler(IDiscountRepository discounts)
{
    public async Task<PagedResult<DiscountDto>> HandleAsync(ListDiscountsQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await discounts.ListAsync(query.Search, page.Skip, page.Take, ct);
        var total = await discounts.CountAsync(query.Search, ct);
        return new PagedResult<DiscountDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static DiscountDto Map(Discount d) =>
        new(d.Id, d.Name, d.Type.ToDbValue(), d.Value, d.StartDate, d.EndDate);
}

public sealed class GetDiscountHandler(IDiscountRepository discounts)
{
    public async Task<DiscountDto> HandleAsync(int id, CancellationToken ct = default)
        => ListDiscountsHandler.Map(await discounts.GetAsync(id, ct) ?? throw new NotFoundException("descuento", id));
}
