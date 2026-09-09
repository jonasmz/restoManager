namespace RestoManager.Business.Domain.Inventory;

/// <summary>
/// Enlace polimórfico de un movimiento con su origen (spec §7.4). Sin FK en BD.
/// </summary>
public readonly record struct MovementReference(string? Type, int? Id)
{
    public static readonly MovementReference None = new(null, null);

    public bool HasValue => Type is not null && Id is not null;

    public static MovementReference To(string type, int id) => new(type, id);
}

/// <summary>Convención de <c>reference_type</c> (spec §7.4 + carga inicial).</summary>
public static class MovementReferenceTypes
{
    public const string PurchaseOrder = "PURCHASE_ORDER";
    public const string Order = "ORDER";
    public const string WasteLog = "WASTE_LOG";
    public const string ManualAdjustment = "MANUAL_ADJUSTMENT";
    public const string InitialLoad = "INITIAL_LOAD";
}
