namespace RestoManager.Business.Application.PublicCatalog;

/// <summary>
/// Carta pública de una sucursal (Fase 11): lo que ve un cliente al escanear el QR.
/// Solo datos comerciales; sin costos, sin unidades de receta, sin stock.
/// </summary>
public sealed record PublicCatalogDto(
    PublicRestaurantDto Restaurant,
    PublicBranchDto Branch,
    IReadOnlyList<PublicCategoryDto> Categories,
    IReadOnlyList<PublicCatalogItemDto> Items);

public sealed record PublicRestaurantDto(string Name);

public sealed record PublicBranchDto(string Name, string Address);

public sealed record PublicCategoryDto(int Id, string Name);

public sealed record PublicCatalogItemDto(
    int Id,
    int CategoryId,
    string CategoryName,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    IReadOnlyList<string> Ingredients);
