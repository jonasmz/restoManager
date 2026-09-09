using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.DiningRoom;

namespace RestoManager.Business.Application.DiningRoom.Tables;

public sealed record TableDto(int Id, int BranchId, int Number, int Capacity, string OperationalStatus);

// ---- Alta / edición (sucursal = sucursal activa) ----
public sealed record SaveTableCommand(int? Id, int Number, int Capacity);

public sealed class SaveTableValidator : AbstractValidator<SaveTableCommand>
{
    public SaveTableValidator()
    {
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.Capacity).GreaterThan(0);
    }
}

public sealed class SaveTableHandler(
    ITableRepository tables,
    IUnitOfWork unitOfWork,
    IBranchContext branchContext,
    BranchAccessGuard access,
    IValidator<SaveTableCommand> validator)
{
    public async Task<int> HandleAsync(SaveTableCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var branchId = branchContext.BranchId;
        access.EnsureCanOperate(branchId);

        if (await tables.NumberExistsAsync(branchId, command.Number, command.Id, ct))
        {
            throw new DomainRuleException(
                "dining.duplicate_table_number",
                $"Ya existe una mesa con el número {command.Number} en la sucursal.");
        }

        Table entity;
        if (command.Id is { } id)
        {
            entity = await tables.GetAsync(id, ct) ?? throw new NotFoundException("mesa", id);
            access.EnsureCanOperate(entity.BranchId);
            entity.UpdateDetails(command.Number, command.Capacity);
        }
        else
        {
            entity = new Table(branchId, command.Number, command.Capacity);
            tables.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

// ---- Cambio de estado operativo ----
public sealed record SetTableStatusCommand(int TableId, string OperationalStatus);

public sealed class SetTableStatusHandler(
    ITableRepository tables,
    ITableSessionRepository sessions,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access)
{
    public async Task HandleAsync(SetTableStatusCommand command, CancellationToken ct = default)
    {
        var table = await tables.GetAsync(command.TableId, ct) ?? throw new NotFoundException("mesa", command.TableId);
        access.EnsureCanOperate(table.BranchId);

        var next = TableOperationalStatusExtensions.FromDbValue(command.OperationalStatus);
        var hasOpenSession = await sessions.GetOpenForTableAsync(table.Id, ct) is not null;
        table.SetOperationalStatus(next, hasOpenSession);

        await unitOfWork.SaveChangesAsync(ct);
    }
}

// ---- Consultas ----
public sealed class ListTablesHandler(ITableRepository tables, IBranchContext branchContext)
{
    public async Task<IReadOnlyList<TableDto>> HandleAsync(CancellationToken ct = default)
    {
        var items = await tables.ListByBranchAsync(branchContext.BranchId, ct);
        return items.Select(Map).ToList();
    }

    internal static TableDto Map(Table t) =>
        new(t.Id, t.BranchId, t.Number, t.Capacity, t.OperationalStatus.ToDbValue());
}

public sealed class GetTableHandler(ITableRepository tables)
{
    public async Task<TableDto> HandleAsync(int id, CancellationToken ct = default)
        => ListTablesHandler.Map(await tables.GetAsync(id, ct) ?? throw new NotFoundException("mesa", id));
}
