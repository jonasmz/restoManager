using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Application.Customers;

// Canje de puntos de fidelidad como descuento de un pedido OPEN (Fase 8, decisión 1).
// La acumulación la dispara CloseOrder (ver CloseOrderHandler en Sales).

public sealed record RedeemLoyaltyPointsCommand(int CustomerId, int OrderId, int Points);

public sealed record LoyaltyRedemptionResult(
    int CustomerId, int OrderId, int PointsRedeemed, int RemainingPoints,
    decimal DiscountAmount, decimal OrderTotal);

public sealed class RedeemLoyaltyPointsValidator : AbstractValidator<RedeemLoyaltyPointsCommand>
{
    public RedeemLoyaltyPointsValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Points).GreaterThan(0);
    }
}

public sealed class RedeemLoyaltyPointsHandler(
    IOrderRepository orders,
    ICustomerRepository customers,
    IDiscountRepository discounts,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    LoyaltyPolicy policy,
    IValidator<RedeemLoyaltyPointsCommand> validator)
{
    public async Task<LoyaltyRedemptionResult> HandleAsync(
        RedeemLoyaltyPointsCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var order = await orders.GetAsync(command.OrderId, ct)
            ?? throw new NotFoundException("pedido", command.OrderId);
        access.EnsureCanOperate(order.BranchId);

        if (order.CustomerId is not { } orderCustomerId || orderCustomerId != command.CustomerId)
        {
            throw new DomainRuleException(
                "loyalty.order_customer_mismatch",
                "El pedido no está identificado con ese cliente.");
        }

        var customer = await customers.GetAsync(command.CustomerId, ct)
            ?? throw new NotFoundException("cliente", command.CustomerId);

        if (command.Points > customer.LoyaltyPoints)
        {
            throw new DomainRuleException(
                "loyalty.insufficient_points",
                $"El cliente solo tiene {customer.LoyaltyPoints} puntos de fidelidad.");
        }

        var loyaltyDiscount = await discounts.GetByNameAsync(LoyaltyPolicy.SystemDiscountName, ct)
            ?? throw new DomainRuleException(
                "loyalty.not_configured",
                "No está configurado el descuento de sistema para el canje de puntos.");

        var amount = Money.Round(policy.AmountFor(command.Points));
        if (amount <= 0m)
        {
            throw new DomainRuleException(
                "loyalty.redemption_too_small",
                $"Se necesitan al menos {policy.RedeemRate} puntos para descontar una unidad monetaria.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async _ =>
        {
            order.ApplyLoyaltyRedemption(loyaltyDiscount.Id, amount);
            customer.RedeemLoyaltyPoints(command.Points);
        }, ct);

        return new LoyaltyRedemptionResult(
            customer.Id, order.Id, command.Points, customer.LoyaltyPoints, amount, order.TotalAmount);
    }
}
