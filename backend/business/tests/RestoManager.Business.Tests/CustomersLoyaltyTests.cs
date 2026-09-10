using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Tests;

public class CustomerLoyaltyPointsTests
{
    private static Customer NewCustomer() => new("Ana", "García", "555-1", "ana@demo.local");

    [Fact]
    public void Add_points_accumulates_and_rejects_negative()
    {
        var c = NewCustomer();
        c.AddLoyaltyPoints(120);
        c.AddLoyaltyPoints(30);
        Assert.Equal(150, c.LoyaltyPoints);

        Assert.Throws<DomainRuleException>(() => c.AddLoyaltyPoints(-1));
    }

    [Fact]
    public void Redeem_points_subtracts_and_guards_balance()
    {
        var c = NewCustomer();
        c.AddLoyaltyPoints(200);

        c.RedeemLoyaltyPoints(150);
        Assert.Equal(50, c.LoyaltyPoints);

        var tooMany = Assert.Throws<DomainRuleException>(() => c.RedeemLoyaltyPoints(51));
        Assert.Equal("loyalty.insufficient_points", tooMany.Code);
        Assert.Throws<DomainRuleException>(() => c.RedeemLoyaltyPoints(0));
    }
}

public class ReviewTests
{
    private static readonly DateOnly Today = new(2026, 9, 9);

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-3)]
    public void Rating_outside_1_to_5_is_rejected(int rating)
    {
        var ex = Assert.Throws<DomainRuleException>(() => Review.Create(1, 1, rating, null, Today));
        Assert.Equal("reviews.invalid_rating", ex.Code);
    }

    [Fact]
    public void Valid_review_keeps_trimmed_comment_or_null()
    {
        var withComment = Review.Create(1, 2, 4, "  Muy bien  ", Today);
        Assert.Equal("Muy bien", withComment.Comment);
        Assert.Equal(4, withComment.Rating);

        var blank = Review.Create(1, 2, 3, "   ", Today);
        Assert.Null(blank.Comment);
    }

    [Fact]
    public void Comment_over_500_chars_is_rejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => Review.Create(1, 1, 5, new string('x', 501), Today));
        Assert.Equal("reviews.comment_too_long", ex.Code);
    }
}

public class GiftCardIssueTests
{
    private static readonly DateOnly Today = new(2026, 9, 9);

    [Fact]
    public void Issue_rounds_balance_and_sets_fields()
    {
        var card = GiftCard.Issue(7, "  GC-9  ", 49.999m, Today.AddYears(1), Today);
        Assert.Equal(7, card.CustomerId);
        Assert.Equal("GC-9", card.CardNumber);
        Assert.Equal(50.00m, card.Balance);
        Assert.False(card.IsExpiredOn(Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Issue_rejects_non_positive_balance(decimal balance)
    {
        var ex = Assert.Throws<DomainRuleException>(() => GiftCard.Issue(1, "GC-1", balance, Today.AddYears(1), Today));
        Assert.Equal("gift_cards.invalid_balance", ex.Code);
    }

    [Fact]
    public void Issue_rejects_past_expiry_and_blank_number()
    {
        Assert.Equal("gift_cards.invalid_expiry",
            Assert.Throws<DomainRuleException>(() => GiftCard.Issue(1, "GC-1", 10m, Today.AddDays(-1), Today)).Code);
        Assert.Equal("gift_cards.card_number_required",
            Assert.Throws<DomainRuleException>(() => GiftCard.Issue(1, "  ", 10m, Today.AddYears(1), Today)).Code);
    }
}

public class OrderLoyaltyRedemptionTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);
    private const int LoyaltyDiscountId = 99;

    private static Order OrderWithSubtotal(decimal subtotal)
    {
        var order = Order.Create(OrderChannel.Barra, 1, 1, Now, null, null, customerId: 5);
        order.AddItem(menuItemId: 10, quantity: 1, unitPrice: subtotal, notes: null);
        return order;
    }

    [Fact]
    public void Redemption_creates_frozen_discount_and_lowers_total()
    {
        var order = OrderWithSubtotal(40m);

        var row = order.ApplyLoyaltyRedemption(LoyaltyDiscountId, 3.50m);

        Assert.Equal(LoyaltyDiscountId, row.DiscountId);
        Assert.Equal(3.50m, row.AppliedAmount);
        Assert.Equal(3.50m, order.DiscountTotal);
        Assert.Equal(36.50m, order.TotalAmount);
    }

    [Fact]
    public void Second_redemption_on_same_order_is_rejected()
    {
        var order = OrderWithSubtotal(40m);
        order.ApplyLoyaltyRedemption(LoyaltyDiscountId, 2m);

        var ex = Assert.Throws<DomainRuleException>(() => order.ApplyLoyaltyRedemption(LoyaltyDiscountId, 1m));
        Assert.Equal("loyalty.already_redeemed", ex.Code);
    }

    [Fact]
    public void Redemption_over_remaining_amount_is_rejected()
    {
        var order = OrderWithSubtotal(10m);

        var ex = Assert.Throws<DomainRuleException>(() => order.ApplyLoyaltyRedemption(LoyaltyDiscountId, 10.01m));
        Assert.Equal("loyalty.redemption_exceeds_total", ex.Code);
    }

    [Fact]
    public void Redemption_only_on_open_order()
    {
        var order = OrderWithSubtotal(10m);
        order.RegisterPayment(PaymentMethod.Cash, 10m, Now); // OPEN -> PAID

        var ex = Assert.Throws<DomainRuleException>(() => order.ApplyLoyaltyRedemption(LoyaltyDiscountId, 1m));
        Assert.Equal("sales.order_not_open", ex.Code);
    }
}

public class LoyaltyPolicyTests
{
    [Fact]
    public void Points_for_order_total_floors_and_scales()
    {
        var policy = new LoyaltyPolicy(PointsPerCurrencyUnit: 1, RedeemRate: 100m);
        Assert.Equal(36, policy.PointsFor(36.90m));
        Assert.Equal(0, policy.PointsFor(0m));

        var x10 = new LoyaltyPolicy(PointsPerCurrencyUnit: 10, RedeemRate: 100m);
        Assert.Equal(360, x10.PointsFor(36.90m));
    }

    [Fact]
    public void Amount_for_points_uses_redeem_rate()
    {
        var policy = new LoyaltyPolicy(PointsPerCurrencyUnit: 1, RedeemRate: 100m);
        Assert.Equal(3.5m, policy.AmountFor(350));
        Assert.Equal(0m, policy.AmountFor(0));
    }
}
