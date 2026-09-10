using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Delivery;

// Módulo Delivery (Fase 7). Reparto a domicilio como extensión del canal DELIVERY.
// DOM-08: un pedido DELIVERY tiene exactamente una fila en `deliveries` y viceversa;
// se garantiza en la aplicación (sin FK inversa ni trigger). `deliveries.driver_id`
// es NOT NULL: toda entrega nace con repartidor asignado.

/// <summary>Tipo de vehículo del repartidor (catálogo aprobado, transversal §2).</summary>
public enum VehicleType
{
    Motorcycle,
    Bicycle,
    Car,
    OnFoot,
}

public static class VehicleTypeExtensions
{
    public static string ToDbValue(this VehicleType type) => type switch
    {
        VehicleType.Motorcycle => "MOTORCYCLE",
        VehicleType.Bicycle => "BICYCLE",
        VehicleType.Car => "CAR",
        VehicleType.OnFoot => "ON_FOOT",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static VehicleType FromDbValue(string value) => value switch
    {
        "MOTORCYCLE" => VehicleType.Motorcycle,
        "BICYCLE" => VehicleType.Bicycle,
        "CAR" => VehicleType.Car,
        "ON_FOOT" => VehicleType.OnFoot,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "delivery_drivers.vehicle_type inválido"),
    };

    public static bool TryFromDbValue(string? value, out VehicleType type)
    {
        switch (value)
        {
            case "MOTORCYCLE": type = VehicleType.Motorcycle; return true;
            case "BICYCLE": type = VehicleType.Bicycle; return true;
            case "CAR": type = VehicleType.Car; return true;
            case "ON_FOOT": type = VehicleType.OnFoot; return true;
            default: type = default; return false;
        }
    }
}

/// <summary>Repartidor. Vinculado a un <c>employee</c> (Fase 2).</summary>
public sealed class DeliveryDriver
{
    public int Id { get; private set; }
    public int EmployeeId { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public string LicensePlate { get; private set; } = string.Empty;

    private DeliveryDriver() { }

    public DeliveryDriver(int employeeId, VehicleType vehicleType, string licensePlate)
    {
        if (employeeId <= 0)
        {
            throw new DomainRuleException("delivery.invalid_employee", "El empleado del repartidor es obligatorio.");
        }
        EmployeeId = employeeId;
        Update(vehicleType, licensePlate);
    }

    public void Update(VehicleType vehicleType, string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new DomainRuleException("delivery.invalid_plate", "La patente del vehículo es obligatoria.");
        }
        VehicleType = vehicleType;
        LicensePlate = licensePlate.Trim();
    }
}

/// <summary>Estado de la entrega (catálogo aprobado, transversal §2).</summary>
public enum DeliveryStatus
{
    Pending,
    Assigned,
    InTransit,
    Delivered,
    Failed,
    Cancelled,
}

public static class DeliveryStatusExtensions
{
    public static string ToDbValue(this DeliveryStatus status) => status switch
    {
        DeliveryStatus.Pending => "PENDING",
        DeliveryStatus.Assigned => "ASSIGNED",
        DeliveryStatus.InTransit => "IN_TRANSIT",
        DeliveryStatus.Delivered => "DELIVERED",
        DeliveryStatus.Failed => "FAILED",
        DeliveryStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static DeliveryStatus FromDbValue(string value) => value switch
    {
        "PENDING" => DeliveryStatus.Pending,
        "ASSIGNED" => DeliveryStatus.Assigned,
        "IN_TRANSIT" => DeliveryStatus.InTransit,
        "DELIVERED" => DeliveryStatus.Delivered,
        "FAILED" => DeliveryStatus.Failed,
        "CANCELLED" => DeliveryStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "deliveries.status inválido"),
    };

    public static bool TryFromDbValue(string? value, out DeliveryStatus status)
    {
        switch (value)
        {
            case "PENDING": status = DeliveryStatus.Pending; return true;
            case "ASSIGNED": status = DeliveryStatus.Assigned; return true;
            case "IN_TRANSIT": status = DeliveryStatus.InTransit; return true;
            case "DELIVERED": status = DeliveryStatus.Delivered; return true;
            case "FAILED": status = DeliveryStatus.Failed; return true;
            case "CANCELLED": status = DeliveryStatus.Cancelled; return true;
            default: status = default; return false;
        }
    }
}

/// <summary>
/// Entrega de un pedido <c>DELIVERY</c>. Se crea junto con el pedido, en la misma
/// transacción, ya con repartidor (decisión Fase 7). <c>actual_time</c> solo se
/// informa al pasar a <see cref="DeliveryStatus.Delivered"/> o
/// <see cref="DeliveryStatus.Failed"/>.
/// </summary>
public sealed class Delivery
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int DriverId { get; private set; }
    public string DeliveryAddress { get; private set; } = string.Empty;
    public DateTime EstimatedTime { get; private set; }
    public DateTime? ActualTime { get; private set; }
    public DeliveryStatus Status { get; private set; }

    private Delivery() { }

    public static Delivery Create(int orderId, int driverId, string deliveryAddress, DateTime estimatedTime)
    {
        if (orderId <= 0)
        {
            throw new DomainRuleException("delivery.invalid_order", "El pedido de la entrega es obligatorio.");
        }
        if (driverId <= 0)
        {
            throw new DomainRuleException("delivery.invalid_driver", "El repartidor es obligatorio.");
        }
        if (string.IsNullOrWhiteSpace(deliveryAddress))
        {
            throw new DomainRuleException("delivery.invalid_address", "La dirección de entrega es obligatoria.");
        }

        return new Delivery
        {
            OrderId = orderId,
            DriverId = driverId,
            DeliveryAddress = deliveryAddress.Trim(),
            EstimatedTime = estimatedTime,
            Status = DeliveryStatus.Pending,
        };
    }

    /// <summary>(Re)asigna el repartidor. Permitido en <c>PENDING</c>, <c>ASSIGNED</c> o <c>FAILED</c> (reintento).</summary>
    public void AssignDriver(int driverId)
    {
        if (driverId <= 0)
        {
            throw new DomainRuleException("delivery.invalid_driver", "El repartidor es obligatorio.");
        }
        if (Status is not (DeliveryStatus.Pending or DeliveryStatus.Assigned or DeliveryStatus.Failed))
        {
            throw Invalid("asignar repartidor");
        }
        DriverId = driverId;
        ActualTime = null;
        Status = DeliveryStatus.Assigned;
    }

    public void MarkInTransit()
    {
        if (Status != DeliveryStatus.Assigned)
        {
            throw Invalid("marcar en camino");
        }
        Status = DeliveryStatus.InTransit;
    }

    public void MarkDelivered(DateTime now)
    {
        if (Status != DeliveryStatus.InTransit)
        {
            throw Invalid("marcar entregada");
        }
        Status = DeliveryStatus.Delivered;
        ActualTime = now;
    }

    public void MarkFailed(DateTime now)
    {
        if (Status is not (DeliveryStatus.Assigned or DeliveryStatus.InTransit))
        {
            throw Invalid("marcar fallida");
        }
        Status = DeliveryStatus.Failed;
        ActualTime = now;
    }

    public void Cancel()
    {
        if (Status is DeliveryStatus.Delivered or DeliveryStatus.Cancelled)
        {
            throw Invalid("cancelar");
        }
        Status = DeliveryStatus.Cancelled;
    }

    private DomainRuleException Invalid(string action) => new(
        "delivery.invalid_transition",
        $"No se puede {action} en una entrega en estado {Status.ToDbValue()}.");
}
