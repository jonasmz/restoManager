using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.DiningRoom;

namespace RestoManager.Business.Application.DiningRoom.Sessions;

public sealed record TableSessionDto(int Id, int TableId, int GuestCount, DateTime OpenedAt, DateTime? ClosedAt);

// ---- Abrir sesión ----
public sealed record OpenSessionCommand(int TableId, int GuestCount);

public sealed class OpenSessionValidator : AbstractValidator<OpenSessionCommand>
{
    public OpenSessionValidator() => RuleFor(x => x.GuestCount).GreaterThan(0);
}

public sealed class OpenSessionHandler(
    ITableRepository tables,
    ITableSessionRepository sessions,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IClock clock,
    IValidator<OpenSessionCommand> validator)
{
    public async Task<int> HandleAsync(OpenSessionCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var table = await tables.GetAsync(command.TableId, ct) ?? throw new NotFoundException("mesa", command.TableId);
        access.EnsureCanOperate(table.BranchId);

        // SES-02 / DOM-03: solo se abre sesión en mesa ACTIVE.
        if (table.OperationalStatus != TableOperationalStatus.Active)
        {
            throw new DomainRuleException(
                "dining.table_not_active",
                $"No se puede abrir una sesión en una mesa en estado {table.OperationalStatus.ToDbValue()}.");
        }

        // SES-01: una sola sesión abierta por mesa (respaldado por índice único parcial).
        if (await sessions.GetOpenForTableAsync(table.Id, ct) is not null)
        {
            throw new DomainRuleException("dining.session_already_open", "La mesa ya tiene una sesión abierta.");
        }

        // SES-05: no más comensales que la capacidad de la mesa.
        var session = TableSession.Open(table.Id, command.GuestCount, table.Capacity, clock.UtcNow);
        sessions.Add(session);
        await unitOfWork.SaveChangesAsync(ct);
        return session.Id;
    }
}

// ---- Cerrar sesión ----
public sealed record CloseSessionCommand(int TableId, int SessionId);

public sealed class CloseSessionHandler(
    ITableRepository tables,
    ITableSessionRepository sessions,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IClock clock)
{
    public async Task HandleAsync(CloseSessionCommand command, CancellationToken ct = default)
    {
        var session = await sessions.GetAsync(command.SessionId, ct)
            ?? throw new NotFoundException("sesión de mesa", command.SessionId);
        if (session.TableId != command.TableId)
        {
            throw new NotFoundException("sesión de mesa", command.SessionId);
        }

        var table = await tables.GetAsync(session.TableId, ct) ?? throw new NotFoundException("mesa", session.TableId);
        access.EnsureCanOperate(table.BranchId);

        session.Close(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
