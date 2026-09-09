using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Menu;

namespace RestoManager.Business.Application.Menu.Stations;

public sealed record KitchenStationDto(
    int Id, int BranchId, string Name, string Description, IReadOnlyList<int> MenuItemIds);

// ---- Estación (CRUD, ámbito sucursal activa) ----
public sealed record SaveKitchenStationCommand(int? Id, string Name, string Description);

public sealed class SaveKitchenStationValidator : AbstractValidator<SaveKitchenStationCommand>
{
    public SaveKitchenStationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public sealed class SaveKitchenStationHandler(
    IKitchenStationRepository stations,
    IBranchContext branchContext,
    IUnitOfWork unitOfWork,
    IValidator<SaveKitchenStationCommand> validator)
{
    public async Task<int> HandleAsync(SaveKitchenStationCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var branchId = branchContext.BranchId;

        KitchenStation entity;
        if (command.Id is { } id)
        {
            entity = await stations.GetAsync(id, ct) ?? throw new NotFoundException("estación de cocina", id);
            if (entity.BranchId != branchId)
            {
                throw new BranchAccessDeniedException(entity.BranchId);
            }
            entity.Update(command.Name, command.Description);
        }
        else
        {
            entity = new KitchenStation(branchId, command.Name, command.Description);
            stations.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed class ListKitchenStationsHandler(IKitchenStationRepository stations, IBranchContext branchContext)
{
    public async Task<IReadOnlyList<KitchenStationDto>> HandleAsync(CancellationToken ct = default)
    {
        var rows = await stations.ListByBranchAsync(branchContext.BranchId, ct);
        return rows.Select(Map).ToList();
    }

    internal static KitchenStationDto Map(KitchenStation s) => new(
        s.Id, s.BranchId, s.Name, s.Description, s.MenuItems.Select(m => m.MenuItemId).ToList());
}

public sealed class GetKitchenStationHandler(IKitchenStationRepository stations, IBranchContext branchContext)
{
    public async Task<KitchenStationDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var s = await stations.GetAsync(id, ct) ?? throw new NotFoundException("estación de cocina", id);
        if (s.BranchId != branchContext.BranchId)
        {
            throw new BranchAccessDeniedException(s.BranchId);
        }
        return ListKitchenStationsHandler.Map(s);
    }
}

// ---- Platos de la estación (sub-recurso, se reemplaza en bloque) ----
public sealed record SetStationMenuItemsCommand(int StationId, IReadOnlyList<int> MenuItemIds);

public sealed class SetStationMenuItemsValidator : AbstractValidator<SetStationMenuItemsCommand>
{
    public SetStationMenuItemsValidator()
    {
        RuleFor(x => x.StationId).GreaterThan(0);
        RuleForEach(x => x.MenuItemIds).GreaterThan(0);
    }
}

public sealed class SetStationMenuItemsHandler(
    IKitchenStationRepository stations,
    IMenuItemRepository menuItems,
    IBranchContext branchContext,
    IUnitOfWork unitOfWork,
    IValidator<SetStationMenuItemsCommand> validator)
{
    public async Task HandleAsync(SetStationMenuItemsCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var station = await stations.GetAsync(command.StationId, ct)
            ?? throw new NotFoundException("estación de cocina", command.StationId);
        if (station.BranchId != branchContext.BranchId)
        {
            throw new BranchAccessDeniedException(station.BranchId);
        }

        foreach (var menuItemId in command.MenuItemIds)
        {
            if (!await menuItems.ExistsAsync(menuItemId, ct))
            {
                throw new NotFoundException("plato", menuItemId);
            }
        }

        station.SetMenuItems(command.MenuItemIds);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
