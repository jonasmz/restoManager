using RestoManager.Business.Domain.Common;
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
    public void Recalculate_subtracts_discount_and_never_goes_negative()
    {
        var order = NewBarOrder();
        order.AddItem(menuItemId: 10, quantity: 1, unitPrice: 20.00m, notes: null);

        order.Recalculate(discountTotal: 5m);
        Assert.Equal(15.00m, order.TotalAmount);

        order.Recalculate(discountTotal: 999m);
        Assert.Equal(0m, order.TotalAmount);
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

public class OrderStatusGuardTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Channel_and_status_round_trip_through_db_values()
    {
        foreach (var c in Enum.GetValues<OrderChannel>())
        {
            Assert.Equal(c, OrderChannelExtensions.FromDbValue(c.ToDbValue()));
        }
        foreach (var s in Enum.GetValues<OrderStatus>())
        {
            Assert.Equal(s, OrderStatusExtensions.FromDbValue(s.ToDbValue()));
        }

        Assert.True(OrderChannelExtensions.TryFromDbValue("MESA", out _));
        Assert.False(OrderChannelExtensions.TryFromDbValue("mesa", out _));
        Assert.False(OrderStatusExtensions.TryFromDbValue(null, out _));
    }

    [Fact]
    public void Money_rounds_half_up()
    {
        Assert.Equal(2.35m, Money.Round(2.345m));
        Assert.Equal(2.34m, Money.Round(2.344m));
        Assert.Equal(-2.35m, Money.Round(-2.345m));
    }
}
