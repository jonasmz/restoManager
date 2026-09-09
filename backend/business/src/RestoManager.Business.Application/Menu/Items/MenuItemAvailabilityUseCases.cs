using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Menu;

namespace RestoManager.Business.Application.Menu.Items;

/// <summary>
/// Disponibilidad efectiva de un plato en la sucursal activa. <see cref="IsOverride"/>
/// indica que hay una fila propia de la sucursal; si es <c>false</c> el valor se hereda
/// de <c>menu_items.is_available</c>.
/// </summary>
public sealed record MenuItemAvailabilityDto(int MenuItemId, int BranchId, bool IsAvailable, bool IsOverride);

public sealed class GetMenuItemAvailabilityHandler(
    IMenuItemRepository menuItems,
    IMenuItemAvailabilityRepository availability,
    IBranchContext branchContext)
{
    public async Task<MenuItemAvailabilityDto> HandleAsync(int menuItemId, CancellationToken ct = default)
    {
        var branchId = branchContext.BranchId;
        var item = await menuItems.GetAsync(menuItemId, ct) ?? throw new NotFoundException("plato", menuItemId);
        var row = await availability.GetAsync(branchId, menuItemId, ct);
        return new MenuItemAvailabilityDto(
            item.Id, branchId, row?.IsAvailable ?? item.IsAvailable, row is not null);
    }
}

public sealed record SetMenuItemAvailabilityCommand(int MenuItemId, bool IsAvailable);

public sealed class SetMenuItemAvailabilityValidator : AbstractValidator<SetMenuItemAvailabilityCommand>
{
    public SetMenuItemAvailabilityValidator() => RuleFor(x => x.MenuItemId).GreaterThan(0);
}

public sealed class SetMenuItemAvailabilityHandler(
    IMenuItemRepository menuItems,
    IMenuItemAvailabilityRepository availability,
    IBranchContext branchContext,
    IUnitOfWork unitOfWork,
    IValidator<SetMenuItemAvailabilityCommand> validator)
{
    public async Task HandleAsync(SetMenuItemAvailabilityCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var branchId = branchContext.BranchId;

        if (!await menuItems.ExistsAsync(command.MenuItemId, ct))
        {
            throw new NotFoundException("plato", command.MenuItemId);
        }

        var row = await availability.GetAsync(branchId, command.MenuItemId, ct);
        if (row is null)
        {
            availability.Add(new MenuItemBranchAvailability(branchId, command.MenuItemId, command.IsAvailable));
        }
        else
        {
            row.Set(command.IsAvailable);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
