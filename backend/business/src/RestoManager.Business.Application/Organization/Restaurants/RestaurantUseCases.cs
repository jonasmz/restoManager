using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Application.Organization.Restaurants;

public sealed record RestaurantDto(int Id, string Name, string Address, string Phone, string Email, string TaxNumber);

public sealed record SaveRestaurantCommand(
    int? Id, string Name, string Address, string Phone, string Email, string TaxNumber);

public sealed class SaveRestaurantValidator : AbstractValidator<SaveRestaurantCommand>
{
    public SaveRestaurantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(100);
        RuleFor(x => x.TaxNumber).MaximumLength(50);
    }
}

public sealed class SaveRestaurantHandler(
    IRestaurantRepository restaurants, IUnitOfWork unitOfWork, IValidator<SaveRestaurantCommand> validator)
{
    public async Task<int> HandleAsync(SaveRestaurantCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        Restaurant entity;
        if (command.Id is { } id)
        {
            entity = await restaurants.GetAsync(id, ct) ?? throw new NotFoundException("restaurante", id);
            entity.Update(command.Name, command.Address, command.Phone, command.Email, command.TaxNumber);
        }
        else
        {
            entity = new Restaurant(command.Name, command.Address, command.Phone, command.Email, command.TaxNumber);
            restaurants.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed record ListRestaurantsQuery(int Page = 1, int PageSize = 20);

public sealed class ListRestaurantsHandler(IRestaurantRepository restaurants)
{
    public async Task<PagedResult<RestaurantDto>> HandleAsync(ListRestaurantsQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await restaurants.ListAsync(page.Skip, page.Take, ct);
        var total = await restaurants.CountAsync(ct);
        return new PagedResult<RestaurantDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static RestaurantDto Map(Restaurant r) => new(r.Id, r.Name, r.Address, r.Phone, r.Email, r.TaxNumber);
}

public sealed class GetRestaurantHandler(IRestaurantRepository restaurants)
{
    public async Task<RestaurantDto> HandleAsync(int id, CancellationToken ct = default)
        => ListRestaurantsHandler.Map(await restaurants.GetAsync(id, ct) ?? throw new NotFoundException("restaurante", id));
}
