namespace RestoManager.Business.Domain.Delivery;

// Módulo Delivery. Solo esquema en la Fase 3. La Fase 7 añade DOM-08 y el
// seguimiento de entregas.

public sealed class DeliveryDriver
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
}

public sealed class Delivery
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int DriverId { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTime EstimatedTime { get; set; }
    public DateTime? ActualTime { get; set; }
    public string Status { get; set; } = string.Empty;
}
