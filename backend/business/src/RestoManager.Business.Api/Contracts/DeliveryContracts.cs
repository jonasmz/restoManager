namespace RestoManager.Business.Api.Contracts;

// Sucursal = sucursal activa (header X-Branch-Id). Las entregas se resuelven por su pedido.

public sealed record SaveDriverRequest(int EmployeeId, string VehicleType, string LicensePlate);

public sealed record AssignDriverRequest(int DriverId);
