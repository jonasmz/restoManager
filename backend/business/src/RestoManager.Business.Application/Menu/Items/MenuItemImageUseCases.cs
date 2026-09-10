using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Menu;

namespace RestoManager.Business.Application.Menu.Items;

/// <summary>Resultado de subir una imagen: su URL pública ya resuelta.</summary>
public sealed record MenuItemImageResult(string ImageUrl);

public sealed record SetMenuItemImageCommand(int MenuItemId, Stream Content, string ContentType, long Length);

/// <summary>
/// Asocia una imagen ilustrativa a un plato (Fase 11). Valida formato y tamaño, la sube
/// al almacén y borra la imagen anterior si la había.
/// </summary>
public sealed class SetMenuItemImageHandler(
    IMenuItemRepository menuItems, IImageStorage storage, IUnitOfWork unitOfWork)
{
    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    private const long MaxBytes = 2 * 1024 * 1024;

    public async Task<MenuItemImageResult> HandleAsync(SetMenuItemImageCommand command, CancellationToken ct = default)
    {
        if (!AllowedTypes.Contains(command.ContentType))
        {
            throw new InvalidInputException(
                "menu.image_type", "Formato de imagen no admitido. Usa JPG, PNG o WebP.");
        }
        if (command.Length <= 0 || command.Length > MaxBytes)
        {
            throw new InvalidInputException(
                "menu.image_size", "La imagen debe pesar entre 1 byte y 2 MB.");
        }

        var item = await menuItems.GetAsync(command.MenuItemId, ct)
            ?? throw new NotFoundException("plato", command.MenuItemId);
        var previousKey = item.ImageKey;

        var key = await storage.SaveAsync(command.Content, command.ContentType, ct);
        item.SetImage(key);
        await unitOfWork.SaveChangesAsync(ct);

        if (previousKey is not null && previousKey != key)
        {
            storage.Delete(previousKey);
        }

        return new MenuItemImageResult(MenuImagePath.For(key)!);
    }
}

/// <summary>Quita la imagen de un plato y la borra del almacén.</summary>
public sealed class ClearMenuItemImageHandler(
    IMenuItemRepository menuItems, IImageStorage storage, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(int menuItemId, CancellationToken ct = default)
    {
        var item = await menuItems.GetAsync(menuItemId, ct)
            ?? throw new NotFoundException("plato", menuItemId);

        var key = item.ImageKey;
        if (key is null)
        {
            return;
        }

        item.ClearImage();
        await unitOfWork.SaveChangesAsync(ct);
        storage.Delete(key);
    }
}
