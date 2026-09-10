using RestoManager.Business.Application.Sales.Orders;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Delivery;

namespace RestoManager.Business.Tests;

public class DeliveryDriverTests
{
    [Fact]
    public void New_driver_rejects_missing_employee_or_blank_plate()
    {
        Assert.Throws<DomainRuleException>(() => new DeliveryDriver(0, VehicleType.Car, "ABC123"));
        Assert.Throws<DomainRuleException>(() => new DeliveryDriver(1, VehicleType.Car, "  "));
    }

    [Fact]
    public void Vehicle_type_round_trips_through_db_value()
    {
        foreach (var v in Enum.GetValues<VehicleType>())
        {
            Assert.Equal(v, VehicleTypeExtensions.FromDbValue(v.ToDbValue()));
        }
        Assert.True(VehicleTypeExtensions.TryFromDbValue("ON_FOOT", out _));
        Assert.False(VehicleTypeExtensions.TryFromDbValue("HORSE", out _));
    }
}

public class DeliveryTests
{
    private static readonly DateTime Now = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Unspecified);

    private static Delivery New() => Delivery.Create(orderId: 5, driverId: 3, "Calle 1", Now.AddMinutes(30));

    [Fact]
    public void Create_starts_pending_without_actual_time()
    {
        var d = New();
        Assert.Equal(DeliveryStatus.Pending, d.Status);
        Assert.Null(d.ActualTime);
        Assert.Equal(3, d.DriverId);
    }

    [Fact]
    public void Happy_path_pending_to_delivered_sets_actual_time_once()
    {
        var d = New();
        d.AssignDriver(3);
        Assert.Equal(DeliveryStatus.Assigned, d.Status);
        d.MarkInTransit();
        Assert.Equal(DeliveryStatus.InTransit, d.Status);
        d.MarkDelivered(Now.AddMinutes(45));
        Assert.Equal(DeliveryStatus.Delivered, d.Status);
        Assert.Equal(Now.AddMinutes(45), d.ActualTime);
    }

    [Fact]
    public void In_transit_requires_assigned()
    {
        var d = New(); // PENDING
        var ex = Assert.Throws<DomainRuleException>(() => d.MarkInTransit());
        Assert.Equal("delivery.invalid_transition", ex.Code);
    }

    [Fact]
    public void Delivered_requires_in_transit()
    {
        var d = New();
        d.AssignDriver(3);
        Assert.Throws<DomainRuleException>(() => d.MarkDelivered(Now));
    }

    [Fact]
    public void Failed_from_assigned_or_in_transit_sets_actual_time()
    {
        var a = New();
        a.AssignDriver(3);
        a.MarkFailed(Now);
        Assert.Equal(DeliveryStatus.Failed, a.Status);
        Assert.Equal(Now, a.ActualTime);
    }

    [Fact]
    public void Assign_driver_allowed_from_pending_assigned_and_failed()
    {
        var d = New();
        d.AssignDriver(7);            // from PENDING
        Assert.Equal(7, d.DriverId);
        d.AssignDriver(8);            // from ASSIGNED (reassign)
        Assert.Equal(8, d.DriverId);
        d.MarkFailed(Now);
        d.AssignDriver(9);            // retry from FAILED
        Assert.Equal(DeliveryStatus.Assigned, d.Status);
        Assert.Null(d.ActualTime);
    }

    [Fact]
    public void Cannot_assign_or_cancel_a_delivered_delivery()
    {
        var d = New();
        d.AssignDriver(3);
        d.MarkInTransit();
        d.MarkDelivered(Now);
        Assert.Throws<DomainRuleException>(() => d.AssignDriver(4));
        Assert.Throws<DomainRuleException>(() => d.Cancel());
    }

    [Fact]
    public void Cancel_allowed_before_delivered()
    {
        var d = New();
        d.Cancel();
        Assert.Equal(DeliveryStatus.Cancelled, d.Status);
        Assert.Throws<DomainRuleException>(() => d.Cancel()); // ya cancelada
    }

    [Fact]
    public void Status_round_trips_through_db_value()
    {
        foreach (var s in Enum.GetValues<DeliveryStatus>())
        {
            Assert.Equal(s, DeliveryStatusExtensions.FromDbValue(s.ToDbValue()));
        }
    }
}

public class CreateOrderDeliveryValidationTests
{
    private static readonly CreateOrderValidator Validator = new();
    private static readonly DateTime Est = new(2026, 9, 10, 13, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Delivery_requires_address_time_and_driver()
    {
        var result = Validator.Validate(new CreateOrderCommand("DELIVERY", null, null, null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.DeliveryAddress));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.EstimatedTime));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.DriverId));
    }

    [Fact]
    public void Delivery_with_all_fields_is_valid()
    {
        var result = Validator.Validate(
            new CreateOrderCommand("DELIVERY", null, null, 4, "Av. Siempre Viva 742", Est, 2));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Non_delivery_channel_rejects_delivery_fields()
    {
        var result = Validator.Validate(
            new CreateOrderCommand("BARRA", null, null, null, "Calle X", Est, 2));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.DeliveryAddress));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.DriverId));
    }
}
