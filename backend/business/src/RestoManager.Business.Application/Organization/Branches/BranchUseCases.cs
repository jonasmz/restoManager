using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Application.Organization.Branches;

public sealed record BranchDto(
    int Id, int RestaurantId, string Name, string Address, string Phone, string Email,
    TimeOnly OpeningTime, TimeOnly ClosingTime);

public sealed record SaveBranchCommand(
    int? Id, int RestaurantId, string Name, string Address, string Phone, string Email,
    TimeOnly OpeningTime, TimeOnly ClosingTime);

public sealed class SaveBranchValidator : AbstractValidator<SaveBranchCommand>
{
    public SaveBranchValidator()
    {
        RuleFor(x => x.RestaurantId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(100);
    }
}

public sealed class SaveBranchHandler(
    IBranchRepository branches,
    IRestaurantRepository restaurants,
    IUnitOfWork unitOfWork,
    IValidator<SaveBranchCommand> validator)
{
    public async Task<int> HandleAsync(SaveBranchCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        if (await restaurants.GetAsync(command.RestaurantId, ct) is null)
        {
            throw new NotFoundException("restaurante", command.RestaurantId);
        }

        Branch entity;
        if (command.Id is { } id)
        {
            entity = await branches.GetAsync(id, ct) ?? throw new NotFoundException("sucursal", id);
            entity.Update(command.Name, command.Address, command.Phone, command.Email, command.OpeningTime, command.ClosingTime);
        }
        else
        {
            entity = new Branch(command.RestaurantId, command.Name, command.Address, command.Phone, command.Email,
                command.OpeningTime, command.ClosingTime);
            branches.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed record ListBranchesQuery(int? RestaurantId, int Page = 1, int PageSize = 20);

public sealed class ListBranchesHandler(IBranchRepository branches)
{
    public async Task<PagedResult<BranchDto>> HandleAsync(ListBranchesQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await branches.ListAsync(query.RestaurantId, page.Skip, page.Take, ct);
        var total = await branches.CountAsync(query.RestaurantId, ct);
        return new PagedResult<BranchDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static BranchDto Map(Branch b) =>
        new(b.Id, b.RestaurantId, b.Name, b.Address, b.Phone, b.Email, b.OpeningTime, b.ClosingTime);
}

public sealed class GetBranchHandler(IBranchRepository branches)
{
    public async Task<BranchDto> HandleAsync(int id, CancellationToken ct = default)
        => ListBranchesHandler.Map(await branches.GetAsync(id, ct) ?? throw new NotFoundException("sucursal", id));
}
