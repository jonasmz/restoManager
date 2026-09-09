using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;

namespace RestoManager.Business.Application.Inventory.Waste;

public sealed record RegisterWasteCommand(int BranchId, int IngredientId, decimal Quantity, string Reason);

public sealed class RegisterWasteValidator : AbstractValidator<RegisterWasteCommand>
{
    public RegisterWasteValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.IngredientId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(255);
    }
}

/// <summary>
/// INV-08 / §11.6: crear el <c>waste_log</c> y postear el movimiento WASTE por la
/// misma cantidad, en una sola transacción.
/// </summary>
public sealed class RegisterWasteHandler(
    IWasteLogRepository wasteLogs,
    IIngredientRepository ingredients,
    InventoryLedger ledger,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    BranchAccessGuard access,
    IValidator<RegisterWasteCommand> validator)
{
    public async Task<int> HandleAsync(RegisterWasteCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        access.EnsureCanOperate(command.BranchId);

        if (!await ingredients.ExistsAsync(command.IngredientId, cancellationToken))
        {
            throw new NotFoundException("ingrediente", command.IngredientId);
        }

        var employeeId = currentUser.EmployeeId;
        var wasteLog = WasteLog.Create(
            command.BranchId, command.IngredientId, command.Quantity, command.Reason, clock.UtcNow, employeeId);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            wasteLogs.Add(wasteLog);
            await unitOfWork.SaveChangesAsync(token); // materializa wasteLog.Id para la referencia del movimiento

            await ledger.PostAsync(
                new LedgerEntry(
                    command.BranchId,
                    command.IngredientId,
                    MovementType.Waste,
                    -command.Quantity,
                    MovementReference.To(MovementReferenceTypes.WasteLog, wasteLog.Id),
                    employeeId),
                token);
        }, cancellationToken);

        return wasteLog.Id;
    }
}

// ---- Listado ----
public sealed record ListWasteLogsQuery(int BranchId, int? IngredientId);

public sealed record WasteLogDto(
    int Id, int IngredientId, decimal Quantity, string Reason, DateTime LoggedTime, int LoggedBy);

public sealed class ListWasteLogsHandler(IWasteLogRepository wasteLogs, BranchAccessGuard access)
{
    public async Task<IReadOnlyList<WasteLogDto>> HandleAsync(ListWasteLogsQuery query, CancellationToken cancellationToken = default)
    {
        access.EnsureCanOperate(query.BranchId);
        var rows = await wasteLogs.ListAsync(query.BranchId, query.IngredientId, cancellationToken);
        return rows.Select(w => new WasteLogDto(w.Id, w.IngredientId, w.Quantity, w.Reason, w.LoggedTime, w.LoggedBy)).ToList();
    }
}
