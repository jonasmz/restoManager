using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Tests;

public class OrderChannelCoherenceTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Mesa_requires_a_table()
    {
        var ex = Assert.Throws<DomainRuleException>(() =>
            Order.Create(OrderChannel.Mesa, branchId: 1, employeeId: 1, Now, tableId: null, tableSessionId: null, customerId: null));
        Assert.Equal("sales.table_required", ex.Code);
    }

    [Fact]
    public void Mesa_keeps_table_and_session()
    {
        var order = Order.Create(
            OrderChannel.Mesa, 1, 1, Now, tableId: 7, tableSessionId: 3, customerId: null);

        Assert.Equal(OrderChannel.Mesa, order.Channel);
        Assert.Equal(7, order.TableId);
        Assert.Equal(3, order.TableSessionId);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0m, order.TotalAmount);
    }

    [Theory]
    [InlineData(OrderChannel.Barra)]
    [InlineData(OrderChannel.Takeaway)]
    [InlineData(OrderChannel.Delivery)]
    public void Non_mesa_channels_reject_table_or_session(OrderChannel channel)
    {
        var withTable = Assert.Throws<DomainRuleException>(() =>
            Order.Create(channel, 1, 1, Now, tableId: 5, tableSessionId: null, customerId: null));
        Assert.Equal("sales.table_not_allowed", withTable.Code);

        Assert.Throws<DomainRuleException>(() =>
            Order.Create(channel, 1, 1, Now, tableId: null, tableSessionId: 9, customerId: null));
    }

    [Fact]
    public void Non_mesa_channel_opens_clean()
    {
        var order = Order.Create(OrderChannel.Takeaway, 1, 1, Now, null, null, customerId: 42);

        Assert.Null(order.TableId);
        Assert.Null(order.TableSessionId);
        Assert.Equal(42, order.CustomerId);
    }

    [Fact]
    public void Create_rejects_non_positive_branch_or_employee()
    {
        Assert.Throws<DomainRuleException>(() =>
            Order.Create(OrderChannel.Barra, branchId: 0, employeeId: 1, Now, null, null, null));
        Assert.Throws<DomainRuleException>(() =>
            Order.Create(OrderChannel.Barra, branchId: 1, employeeId: 0, Now, null, null, null));
    }
}

public class OrderTotalTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);

    private static Order NewBarOrder() =>
        Order.Create(OrderChannel.Barra, 1, 1, Now, null, null, null);

    [Fact]
    public void Total_is_sum_of_line_totals()
    {
        var order = NewBarOrder();
        order.AddItem(menuItemId: 10, quantity: 2, unitPrice: 12.00m, notes: null);
        order.AddItem(menuItemId: 11, quantity: 3, unitPrice: 4.50m, notes: "sin hielo");

        Assert.Equal(24.00m + 13.50m, order.TotalAmount);
        Assert.Equal(37.50m, order.ItemsSubtotal);
    }

    [Fact]
    public void Line_total_and_order_total_round_half_up_to_two_decimals()
    {
        var order = NewBarOrder();
        // 3 × 3.335 = 10.005 → línea 10.01 (medio-arriba).
        order.AddItem(menuItemId: 10, quantity: 3, unitPrice: 3.335m, notes: null);

        Assert.Equal(10.01m, order.Items[0].LineTotal);
        Assert.Equal(10.01m, order.TotalAmount);
    }

    [Fact]
    public void Add_update_remove_keep_total_in_sync()
    {
        var order = NewBarOrder();
        order.AddItem(menuItemId: 10, quantity: 1, unitPrice: 10.00m, notes: null);
        Assert.Equal(10.00m, order.TotalAmount);

        order.UpdateItem(orderItemId: 0, quantity: 4, notes: "para llevar");
        Assert.Equal(40.00m, order.TotalAmount);
        Assert.Equal("para llevar", order.Items[0].Notes);

        order.RemoveItem(orderItemId: 0);
        Assert.Empty(order.Items);
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void Item_rejects_non_positive_quantity_and_negative_price()
    {
        var order = NewBarOrder();
        Assert.Throws<DomainRuleException>(() => order.AddItem(10, quantity: 0, unitPrice: 5m, notes: null));
        Assert.Throws<DomainRuleException>(() => order.AddItem(10, quantity: 1, unitPrice: -0.01m, notes: null));
    }

    [Fact]
    public void Remove_unknown_item_is_not_found()
        => Assert.Throws<NotFoundException>(() => NewBarOrder().RemoveItem(orderItemId: 123));
}

public class DiscountTests
{
    private static readonly DateOnly Today = new(2026, 9, 9);

    [Fact]
    public void Percentage_over_100_is_rejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() =>
            new Discount("x", DiscountType.Percentage, 120m, Today, Today.AddDays(1)));
        Assert.Equal("sales.discount_invalid_percentage", ex.Code);
    }

    [Fact]
    public void Non_positive_value_and_inverted_range_are_rejected()
    {
        Assert.Throws<DomainRuleException>(() => new Discount("x", DiscountType.FixedAmount, 0m, Today, Today));
        Assert.Throws<DomainRuleException>(() =>
            new Discount("x", DiscountType.FixedAmount, 5m, Today, Today.AddDays(-1)));
    }

    [Fact]
    public void IsActiveOn_respects_the_window()
    {
        var d = new Discount("x", DiscountType.FixedAmount, 5m, Today, Today.AddDays(2));
        Assert.False(d.IsActiveOn(Today.AddDays(-1)));
        Assert.True(d.IsActiveOn(Today));
        Assert.True(d.IsActiveOn(Today.AddDays(2)));
        Assert.False(d.IsActiveOn(Today.AddDays(3)));
    }

    [Fact]
    public void ComputeApplied_percentage_and_fixed()
    {
        Assert.Equal(2.50m, new Discount("p", DiscountType.Percentage, 10m, Today, Today).ComputeApplied(25m));
        Assert.Equal(5m, new Discount("f", DiscountType.FixedAmount, 5m, Today, Today).ComputeApplied(25m));
    }

    [Fact]
    public void Type_round_trips_through_db_value()
    {
        foreach (var t in Enum.GetValues<DiscountType>())
        {
            Assert.Equal(t, DiscountTypeExtensions.FromDbValue(t.ToDbValue()));
        }
    }
}

public class OrderDiscountTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private static Order OrderWithSubtotal(decimal subtotal)
    {
        var order = Order.Create(OrderChannel.Barra, 1, 1, Now, null, null, null);
        order.AddItem(menuItemId: 10, quantity: 1, unitPrice: subtotal, notes: null);
        return order;
    }

    private static Discount Pct(int id, decimal pct)
    {
        var d = new Discount($"pct{id}", DiscountType.Percentage, pct, Today.AddDays(-1), Today.AddDays(1));
        typeof(Discount).GetProperty(nameof(Discount.Id))!.SetValue(d, id);
        return d;
    }

    [Fact]
    public void Applying_a_percentage_discount_lowers_the_total()
    {
        var order = OrderWithSubtotal(100m);
        order.ApplyDiscount(Pct(1, 15m), Today);

        Assert.Equal(15m, order.DiscountTotal);
        Assert.Equal(85m, order.TotalAmount);
    }

    [Fact]
    public void Multiple_discounts_sum_and_never_drive_total_negative()
    {
        var order = OrderWithSubtotal(20m);
        order.ApplyDiscount(Pct(1, 80m), Today);   // 16
        order.ApplyDiscount(Pct(2, 50m), Today);   // 10 bruto, topado a 4 restante

        Assert.Equal(20m, order.DiscountTotal);
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void Same_discount_twice_is_rejected()
    {
        var order = OrderWithSubtotal(50m);
        order.ApplyDiscount(Pct(7, 10m), Today);
        var ex = Assert.Throws<DomainRuleException>(() => order.ApplyDiscount(Pct(7, 10m), Today));
        Assert.Equal("sales.discount_duplicate", ex.Code);
    }

    [Fact]
    public void Discount_not_active_is_rejected()
    {
        var order = OrderWithSubtotal(50m);
        var expired = new Discount("old", DiscountType.FixedAmount, 5m, Today.AddDays(-10), Today.AddDays(-5));
        typeof(Discount).GetProperty(nameof(Discount.Id))!.SetValue(expired, 9);
        var ex = Assert.Throws<DomainRuleException>(() => order.ApplyDiscount(expired, Today));
        Assert.Equal("sales.discount_not_active", ex.Code);
    }

    [Fact]
    public void Removing_a_discount_recalculates()
    {
        var order = OrderWithSubtotal(100m);
        order.ApplyDiscount(Pct(1, 15m), Today);
        order.RemoveDiscount(1);

        Assert.Empty(order.Discounts);
        Assert.Equal(100m, order.TotalAmount);
    }
}

public class OrderPaymentTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);

    private static Order OrderTotalling(decimal amount)
    {
        var order = Order.Create(OrderChannel.Barra, 1, 1, Now, null, null, null);
        order.AddItem(menuItemId: 10, quantity: 1, unitPrice: amount, notes: null);
        return order;
    }

    [Fact]
    public void First_confirmed_payment_moves_order_to_paid()
    {
        var order = OrderTotalling(30m);
        order.RegisterPayment(PaymentMethod.Cash, 10m, Now);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(10m, order.ConfirmedPaid);
        Assert.Equal(20m, order.Balance);
    }

    [Fact]
    public void Split_payments_are_allowed_up_to_the_total()
    {
        var order = OrderTotalling(30m);
        order.RegisterPayment(PaymentMethod.Cash, 20m, Now);
        order.RegisterPayment(PaymentMethod.Card, 10m, Now);

        Assert.Equal(30m, order.ConfirmedPaid);
        Assert.Equal(0m, order.Balance);
    }

    [Fact]
    public void Payment_over_the_total_is_rejected()
    {
        var order = OrderTotalling(30m);
        order.RegisterPayment(PaymentMethod.Cash, 25m, Now);
        var ex = Assert.Throws<DomainRuleException>(() => order.RegisterPayment(PaymentMethod.Card, 10m, Now));
        Assert.Equal("sales.payment_exceeds_total", ex.Code);
    }

    [Fact]
    public void Cannot_pay_a_cancelled_order()
    {
        var order = OrderTotalling(30m);
        order.CancelOrder();
        Assert.Throws<DomainRuleException>(() => order.RegisterPayment(PaymentMethod.Cash, 10m, Now));
    }
}

public class OrderLifecycleTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private static Order OrderTotalling(decimal amount)
    {
        var order = Order.Create(OrderChannel.Barra, 1, 1, Now, null, null, null);
        order.AddItem(menuItemId: 10, quantity: 1, unitPrice: amount, notes: null);
        return order;
    }

    [Fact]
    public void Close_requires_full_payment()
    {
        var order = OrderTotalling(30m);
        order.RegisterPayment(PaymentMethod.Cash, 20m, Now);
        var ex = Assert.Throws<DomainRuleException>(() => order.CloseOrder());
        Assert.Equal("sales.order_underpaid", ex.Code);

        order.RegisterPayment(PaymentMethod.Card, 10m, Now);
        order.CloseOrder();
        Assert.Equal(OrderStatus.Closed, order.Status);
    }

    [Fact]
    public void A_fully_discounted_order_closes_without_payment()
    {
        var order = OrderTotalling(20m);
        var d = new Discount("all", DiscountType.Percentage, 100m, Today.AddDays(-1), Today.AddDays(1));
        typeof(Discount).GetProperty(nameof(Discount.Id))!.SetValue(d, 3);
        order.ApplyDiscount(d, Today);

        Assert.Equal(0m, order.TotalAmount);
        order.CloseOrder();
        Assert.Equal(OrderStatus.Closed, order.Status);
    }

    [Fact]
    public void Cancel_refunds_confirmed_payments()
    {
        var order = OrderTotalling(30m);
        order.RegisterPayment(PaymentMethod.Cash, 30m, Now);
        order.CancelOrder();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.All(order.Payments, p => Assert.Equal(PaymentStatus.Refunded, p.Status));
        Assert.Equal(0m, order.ConfirmedPaid);
    }

    [Fact]
    public void Cannot_cancel_a_closed_order()
    {
        var order = OrderTotalling(0m); // sin ítems que paguen
        order.CloseOrder();
        var ex = Assert.Throws<DomainRuleException>(() => order.CancelOrder());
        Assert.Equal("sales.order_closed", ex.Code);
    }
}

public class GiftCardRedeemTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);

    private static GiftCard CardWith(decimal balance, DateOnly? expiry = null) =>
        GiftCard.Issue(
            customerId: 1, cardNumber: "GC-1", initialBalance: balance,
            expiryDate: expiry ?? new DateOnly(2030, 1, 1), today: new DateOnly(2026, 1, 1));

    [Fact]
    public void Redeem_decrements_balance_and_returns_negative_transaction()
    {
        var card = CardWith(50m);
        var txn = card.Redeem(orderId: 7, amount: 20m, Now);

        Assert.Equal(30m, card.Balance);
        Assert.Equal(-20m, txn.Amount);
        Assert.Equal(7, txn.OrderId);
    }

    [Fact]
    public void Redeem_over_balance_is_rejected()
    {
        var card = CardWith(10m);
        var ex = Assert.Throws<DomainRuleException>(() => card.Redeem(1, 25m, Now));
        Assert.Equal("sales.gift_card_insufficient", ex.Code);
    }

    [Fact]
    public void Redeem_on_expired_card_is_rejected()
    {
        var card = CardWith(50m, expiry: new DateOnly(2026, 1, 1)); // caducó antes de Now
        var ex = Assert.Throws<DomainRuleException>(() => card.Redeem(1, 10m, Now));
        Assert.Equal("sales.gift_card_expired", ex.Code);
    }
}

public class SalesEnumTests
{
    [Fact]
    public void Channel_status_and_method_round_trip_through_db_values()
    {
        foreach (var c in Enum.GetValues<OrderChannel>())
        {
            Assert.Equal(c, OrderChannelExtensions.FromDbValue(c.ToDbValue()));
        }
        foreach (var s in Enum.GetValues<OrderStatus>())
        {
            Assert.Equal(s, OrderStatusExtensions.FromDbValue(s.ToDbValue()));
        }
        foreach (var m in Enum.GetValues<PaymentMethod>())
        {
            Assert.Equal(m, PaymentMethodExtensions.FromDbValue(m.ToDbValue()));
        }
        foreach (var p in Enum.GetValues<PaymentStatus>())
        {
            Assert.Equal(p, PaymentStatusExtensions.FromDbValue(p.ToDbValue()));
        }

        Assert.True(PaymentMethodExtensions.TryFromDbValue("GIFT_CARD", out _));
        Assert.False(PaymentMethodExtensions.TryFromDbValue("bitcoin", out _));
    }

    [Fact]
    public void Money_rounds_half_up()
    {
        Assert.Equal(2.35m, Money.Round(2.345m));
        Assert.Equal(2.34m, Money.Round(2.344m));
        Assert.Equal(-2.35m, Money.Round(-2.345m));
    }
}
