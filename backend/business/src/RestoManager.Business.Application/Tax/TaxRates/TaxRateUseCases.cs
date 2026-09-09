using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Tax;

namespace RestoManager.Business.Application.Tax.TaxRates;

public sealed record TaxRateDto(int Id, string Name, decimal Rate);

public sealed record SaveTaxRateCommand(int? Id, string Name, decimal Rate);

public sealed class SaveTaxRateValidator : AbstractValidator<SaveTaxRateCommand>
{
    public SaveTaxRateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Rate).InclusiveBetween(0, 100);
    }
}

public sealed class SaveTaxRateHandler(
    ITaxRateRepository taxRates,
    IUnitOfWork unitOfWork,
    IValidator<SaveTaxRateCommand> validator)
{
    public async Task<int> HandleAsync(SaveTaxRateCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        TaxRate entity;
        if (command.Id is { } id)
        {
            entity = await taxRates.GetAsync(id, ct) ?? throw new NotFoundException("tasa de impuesto", id);
            entity.Update(command.Name, command.Rate);
        }
        else
        {
            entity = new TaxRate(command.Name, command.Rate);
            taxRates.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed record ListTaxRatesQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListTaxRatesHandler(ITaxRateRepository taxRates)
{
    public async Task<PagedResult<TaxRateDto>> HandleAsync(ListTaxRatesQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await taxRates.ListAsync(query.Search, page.Skip, page.Take, ct);
        var total = await taxRates.CountAsync(query.Search, ct);
        return new PagedResult<TaxRateDto>(
            items.Select(t => new TaxRateDto(t.Id, t.Name, t.Rate)).ToList(), query.Page, page.Take, total);
    }
}

public sealed class GetTaxRateHandler(ITaxRateRepository taxRates)
{
    public async Task<TaxRateDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var t = await taxRates.GetAsync(id, ct) ?? throw new NotFoundException("tasa de impuesto", id);
        return new TaxRateDto(t.Id, t.Name, t.Rate);
    }
}
