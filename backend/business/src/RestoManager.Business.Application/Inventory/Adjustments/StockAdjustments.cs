using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;

namespace RestoManager.Business.Application.Inventory.Adjustments;

// ---- Ajuste manual (ADJUSTMENT, cantidad firmada) ----
public sealed record AdjustStockCommand(int IngredientId, decimal Quantity, string Reason);

public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.IngredientId).GreaterThan(0);
        RuleFor(x => x.Quantity).NotEqual(0).WithMessage("La cantidad del ajuste no puede ser cero.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(255);
    }
}

public sealed class AdjustStockHandler(
    IIngredientRepository ingredients,
    InventoryLedger ledger,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IBranchContext branchContext,
    IValidator<AdjustStockCommand> validator)
{
    public async Task HandleAsync(AdjustStockCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var branchId = branchContext.BranchId;

        if (!await ingredients.ExistsAsync(command.IngredientId, cancellationToken))
        {
            throw new NotFoundException("ingrediente", command.IngredientId);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await ledger.PostAsync(
                new LedgerEntry(
                    branchId,
                    command.IngredientId,
                    MovementType.Adjustment,
                    command.Quantity,
                    new MovementReference(MovementReferenceTypes.ManualAdjustment, null),
                    currentUser.EmployeeId),
                token);
        }, cancellationToken);
    }
}

// ---- Carga inicial (ADJUSTMENT + reference INITIAL_LOAD, cantidad positiva) ----
public sealed record LoadInitialStockCommand(int IngredientId, decimal Quantity);

public sealed class LoadInitialStockValidator : AbstractValidator<LoadInitialStockCommand>
{
    public LoadInitialStockValidator()
    {
        RuleFor(x => x.IngredientId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class LoadInitialStockHandler(
    IIngredientRepository ingredients,
    IBranchInventoryRepository balances,
    InventoryLedger ledger,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IBranchContext branchContext,
    IValidator<LoadInitialStockCommand> validator)
{
    public async Task HandleAsync(LoadInitialStockCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var branchId = branchContext.BranchId;

        if (!await ingredients.ExistsAsync(command.IngredientId, cancellationToken))
        {
            throw new NotFoundException("ingrediente", command.IngredientId);
        }

        var existing = await balances.GetAsync(branchId, command.IngredientId, cancellationToken);
        if (existing is { StockQuantity: > 0 })
        {
            throw new DomainRuleException(
                "inventory.already_loaded",
                "Ya existe saldo para ese ingrediente en la sucursal; usa un ajuste (ADJUSTMENT).");
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await ledger.PostAsync(
                new LedgerEntry(
                    branchId,
                    command.IngredientId,
                    MovementType.Adjustment,
                    command.Quantity,
                    new MovementReference(MovementReferenceTypes.InitialLoad, null),
                    currentUser.EmployeeId),
                token);
        }, cancellationToken);
    }
}
