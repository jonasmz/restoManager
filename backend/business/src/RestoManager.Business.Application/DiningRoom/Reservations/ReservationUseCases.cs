using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.DiningRoom;

namespace RestoManager.Business.Application.DiningRoom.Reservations;

public sealed record ReservationDto(
    int Id, int CustomerId, int BranchId, int TableId, DateTime ReservationTime, int PartySize, string Status);

// ---- Crear (PENDING; sucursal = sucursal activa) ----
public sealed record CreateReservationCommand(int CustomerId, int TableId, DateTime ReservationTime, int PartySize);

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.TableId).GreaterThan(0);
        RuleFor(x => x.PartySize).GreaterThan(0);
        RuleFor(x => x.ReservationTime).NotEmpty();
    }
}

public sealed class CreateReservationHandler(
    IReservationRepository reservations,
    ITableRepository tables,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IBranchContext branchContext,
    BranchAccessGuard access,
    ReservationWindow window,
    IValidator<CreateReservationCommand> validator)
{
    public async Task<int> HandleAsync(CreateReservationCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var branchId = branchContext.BranchId;
        access.EnsureCanOperate(branchId);

        if (!await customers.ExistsAsync(command.CustomerId, ct))
        {
            throw new NotFoundException("cliente", command.CustomerId);
        }

        var table = await tables.GetAsync(command.TableId, ct) ?? throw new NotFoundException("mesa", command.TableId);

        // RSV-01 / DOM-04: la reserva vive en la misma sucursal que la mesa.
        if (table.BranchId != branchId)
        {
            throw new DomainRuleException(
                "dining.reservation_branch_mismatch",
                "La mesa no pertenece a la sucursal activa.");
        }

        // Conflicto: otra reserva activa para la misma mesa cuya ventana se solapa.
        var span = window.Before + window.After;
        if (await reservations.HasOverlappingAsync(
                command.TableId,
                command.ReservationTime - span,
                command.ReservationTime + span,
                excludeId: null,
                ct))
        {
            throw new DomainRuleException(
                "dining.reservation_overlap",
                "Ya hay una reserva para esa mesa en un horario solapado.");
        }

        var reservation = Reservation.Create(
            command.CustomerId, branchId, command.TableId, command.ReservationTime, command.PartySize);
        reservations.Add(reservation);
        await unitOfWork.SaveChangesAsync(ct);
        return reservation.Id;
    }
}

// ---- Transiciones ----
public sealed class ConfirmReservationHandler(
    IReservationRepository reservations, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var r = await reservations.GetAsync(id, ct) ?? throw new NotFoundException("reserva", id);
        access.EnsureCanOperate(r.BranchId);
        r.Confirm();
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class CancelReservationHandler(
    IReservationRepository reservations, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var r = await reservations.GetAsync(id, ct) ?? throw new NotFoundException("reserva", id);
        access.EnsureCanOperate(r.BranchId);
        r.Cancel();
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class MarkNoShowHandler(
    IReservationRepository reservations, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var r = await reservations.GetAsync(id, ct) ?? throw new NotFoundException("reserva", id);
        access.EnsureCanOperate(r.BranchId);
        r.MarkNoShow();
        await unitOfWork.SaveChangesAsync(ct);
    }
}

// ---- Sentar (§11.3): abre sesión desde la reserva, todo en una transacción ----
public sealed record SeatReservationCommand(int ReservationId, int GuestCount);

public sealed class SeatReservationValidator : AbstractValidator<SeatReservationCommand>
{
    public SeatReservationValidator() => RuleFor(x => x.GuestCount).GreaterThan(0);
}

public sealed class SeatReservationHandler(
    IReservationRepository reservations,
    ITableRepository tables,
    ITableSessionRepository sessions,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IClock clock,
    IValidator<SeatReservationCommand> validator)
{
    public async Task<int> HandleAsync(SeatReservationCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var reservation = await reservations.GetAsync(command.ReservationId, ct)
            ?? throw new NotFoundException("reserva", command.ReservationId);
        access.EnsureCanOperate(reservation.BranchId);

        if (reservation.Status != ReservationStatus.Confirmed)
        {
            throw new DomainRuleException(
                "dining.reservation_not_confirmed",
                $"No se puede sentar una reserva en estado {reservation.Status.ToDbValue()}.");
        }

        var table = await tables.GetAsync(reservation.TableId, ct)
            ?? throw new NotFoundException("mesa", reservation.TableId);
        if (table.OperationalStatus != TableOperationalStatus.Active)
        {
            throw new DomainRuleException(
                "dining.table_not_active",
                $"La mesa está en estado {table.OperationalStatus.ToDbValue()}.");
        }
        if (await sessions.GetOpenForTableAsync(table.Id, ct) is not null)
        {
            throw new DomainRuleException("dining.session_already_open", "La mesa ya tiene una sesión abierta.");
        }

        var session = TableSession.Open(table.Id, command.GuestCount, clock.UtcNow);

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            sessions.Add(session);
            reservation.MarkSeated();
            return Task.CompletedTask;
        }, ct);

        return session.Id;
    }
}

// ---- Consulta (sucursal activa) ----
public sealed record ListReservationsQuery(DateOnly? Date, string? Status);

public sealed class ListReservationsHandler(IReservationRepository reservations, IBranchContext branchContext)
{
    public async Task<IReadOnlyList<ReservationDto>> HandleAsync(
        ListReservationsQuery query, CancellationToken ct = default)
    {
        DateTime? from = query.Date is { } d
            ? DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified)
            : null;
        DateTime? to = from?.AddDays(1);
        ReservationStatus? status = query.Status is null ? null : ReservationStatusExtensions.FromDbValue(query.Status);

        var items = await reservations.ListAsync(branchContext.BranchId, from, to, status, ct);
        return items.Select(Map).ToList();
    }

    internal static ReservationDto Map(Reservation r) => new(
        r.Id, r.CustomerId, r.BranchId, r.TableId, r.ReservationTime, r.PartySize, r.Status.ToDbValue());
}

public sealed class GetReservationHandler(IReservationRepository reservations)
{
    public async Task<ReservationDto> HandleAsync(int id, CancellationToken ct = default)
        => ListReservationsHandler.Map(await reservations.GetAsync(id, ct) ?? throw new NotFoundException("reserva", id));
}
