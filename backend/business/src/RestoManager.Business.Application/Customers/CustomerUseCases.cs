using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Application.Customers;

// La Fase 5 añadió el alta rápida; la Fase 8 se hace dueña del módulo (fidelización,
// gift cards, reseñas). Catálogo global (sin sucursal).

public sealed record CustomerDto(int Id, string FirstName, string LastName, string Phone, string Email, int LoyaltyPoints);

public sealed record SaveCustomerCommand(int? Id, string FirstName, string LastName, string Phone, string Email);

public sealed class SaveCustomerValidator : AbstractValidator<SaveCustomerCommand>
{
    public SaveCustomerValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(100);
    }
}

public sealed class SaveCustomerHandler(
    ICustomerRepository customers, IUnitOfWork unitOfWork, IValidator<SaveCustomerCommand> validator)
{
    public async Task<int> HandleAsync(SaveCustomerCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        Customer entity;
        if (command.Id is { } id)
        {
            entity = await customers.GetAsync(id, ct) ?? throw new NotFoundException("cliente", id);
            entity.Update(command.FirstName, command.LastName, command.Phone, command.Email);
        }
        else
        {
            entity = new Customer(command.FirstName, command.LastName, command.Phone, command.Email);
            customers.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed record ListCustomersQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListCustomersHandler(ICustomerRepository customers)
{
    public async Task<PagedResult<CustomerDto>> HandleAsync(ListCustomersQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await customers.ListAsync(query.Search, page.Skip, page.Take, ct);
        var total = await customers.CountAsync(query.Search, ct);
        return new PagedResult<CustomerDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static CustomerDto Map(Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.Phone, c.Email, c.LoyaltyPoints);
}

public sealed class GetCustomerHandler(ICustomerRepository customers)
{
    public async Task<CustomerDto> HandleAsync(int id, CancellationToken ct = default)
        => ListCustomersHandler.Map(await customers.GetAsync(id, ct) ?? throw new NotFoundException("cliente", id));
}

// ─────────────────────────── Historial de pedidos (Fase 8) ───────────────────────────

public sealed record CustomerOrderDto(
    int Id, int BranchId, string Channel, string Status, decimal TotalAmount, DateTime OrderTime);

public sealed record ListCustomerOrdersQuery(int CustomerId, int Page = 1, int PageSize = 20);

public sealed class ListCustomerOrdersHandler(ICustomerRepository customers, IOrderRepository orders)
{
    public async Task<PagedResult<CustomerOrderDto>> HandleAsync(
        ListCustomerOrdersQuery query, CancellationToken ct = default)
    {
        if (!await customers.ExistsAsync(query.CustomerId, ct))
        {
            throw new NotFoundException("cliente", query.CustomerId);
        }

        var page = new PageRequest(query.Page, query.PageSize);
        var items = await orders.ListForCustomerAsync(query.CustomerId, page.Skip, page.Take, ct);
        var total = await orders.CountForCustomerAsync(query.CustomerId, ct);

        return new PagedResult<CustomerOrderDto>(
            items.Select(o => new CustomerOrderDto(
                o.Id, o.BranchId, o.Channel.ToDbValue(), o.Status.ToDbValue(), o.TotalAmount, o.OrderTime)).ToList(),
            query.Page, page.Take, total);
    }
}

// ─────────────────────────── Saldo de puntos (Fase 8) ───────────────────────────

public sealed record LoyaltyBalanceDto(int CustomerId, int LoyaltyPoints);

public sealed class GetCustomerLoyaltyHandler(ICustomerRepository customers)
{
    public async Task<LoyaltyBalanceDto> HandleAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await customers.GetAsync(customerId, ct) ?? throw new NotFoundException("cliente", customerId);
        return new LoyaltyBalanceDto(customer.Id, customer.LoyaltyPoints);
    }
}
