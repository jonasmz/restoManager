namespace RestoManager.Business.Api.Contracts;

// Sucursal = sucursal activa (header X-Branch-Id).

// ---- Mesas ----
public sealed record SaveTableRequest(int Number, int Capacity);
public sealed record SetTableStatusRequest(string OperationalStatus);

// ---- Sesiones ----
public sealed record OpenSessionRequest(int GuestCount);

// ---- Reservas ----
public sealed record CreateReservationRequest(int CustomerId, int TableId, DateTime ReservationTime, int PartySize);
public sealed record SeatReservationRequest(int GuestCount);

// ---- Clientes (alta rápida) ----
public sealed record SaveCustomerRequest(string FirstName, string LastName, string Phone, string Email);
