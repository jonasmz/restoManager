using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Purchasing;

namespace RestoManager.Business.Application.Purchasing.Suppliers;

public sealed record SupplierDto(int Id, string Name, string ContactName, string Phone, string Email, string Address);

public sealed record SaveSupplierCommand(
    int? Id, string Name, string ContactName, string Phone, string Email, string Address);

public sealed class SaveSupplierValidator : AbstractValidator<SaveSupplierCommand>
{
    public SaveSupplierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactName).MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(255);
    }
}

public sealed class SaveSupplierHandler(
    ISupplierRepository suppliers,
    IUnitOfWork unitOfWork,
    IValidator<SaveSupplierCommand> validator)
{
    public async Task<int> HandleAsync(SaveSupplierCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        Supplier supplier;
        if (command.Id is { } id)
        {
            supplier = await suppliers.GetAsync(id, cancellationToken) ?? throw new NotFoundException("proveedor", id);
            supplier.Update(command.Name, command.ContactName, command.Phone, command.Email, command.Address);
        }
        else
        {
            supplier = new Supplier(command.Name, command.ContactName, command.Phone, command.Email, command.Address);
            suppliers.Add(supplier);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return supplier.Id;
    }
}

public sealed record ListSuppliersQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListSuppliersHandler(ISupplierRepository suppliers)
{
    public async Task<PagedResult<SupplierDto>> HandleAsync(ListSuppliersQuery query, CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await suppliers.ListAsync(query.Search, page.Skip, page.Take, cancellationToken);
        var total = await suppliers.CountAsync(query.Search, cancellationToken);
        return new PagedResult<SupplierDto>(
            items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static SupplierDto Map(Supplier s) =>
        new(s.Id, s.Name, s.ContactName, s.Phone, s.Email, s.Address);
}

public sealed class GetSupplierHandler(ISupplierRepository suppliers)
{
    public async Task<SupplierDto> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await suppliers.GetAsync(id, cancellationToken) ?? throw new NotFoundException("proveedor", id);
        return ListSuppliersHandler.Map(s);
    }
}
