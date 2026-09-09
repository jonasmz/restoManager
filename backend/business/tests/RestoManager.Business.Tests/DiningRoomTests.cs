using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.DiningRoom;

namespace RestoManager.Business.Tests;

public class TableStatusResolverTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);
    private static readonly ReservationWindow Window = ReservationWindow.Default; // 30 / 30

    [Fact]
    public void OutOfService_wins_over_open_session_and_reservation()
    {
        Assert.Equal(
            TableDisplayStatus.OutOfService,
            TableStatusResolver.Resolve(TableOperationalStatus.OutOfService, hasOpenSession: true, hasReservationInWindow: true));
    }

    [Fact]
    public void Cleaning_wins_over_open_session_and_reservation()
    {
        Assert.Equal(
            TableDisplayStatus.Cleaning,
            TableStatusResolver.Resolve(TableOperationalStatus.Cleaning, hasOpenSession: true, hasReservationInWindow: true));
    }

    [Fact]
    public void Active_with_open_session_is_Occupied_even_with_reservation()
    {
        Assert.Equal(
            TableDisplayStatus.Occupied,
            TableStatusResolver.Resolve(TableOperationalStatus.Active, hasOpenSession: true, hasReservationInWindow: true));
    }

    [Fact]
    public void Active_no_session_with_reservation_is_Reserved()
    {
        Assert.Equal(
            TableDisplayStatus.Reserved,
            TableStatusResolver.Resolve(TableOperationalStatus.Active, hasOpenSession: false, hasReservationInWindow: true));
    }

    [Fact]
    public void Active_no_session_no_reservation_is_Available()
    {
        Assert.Equal(
            TableDisplayStatus.Available,
            TableStatusResolver.Resolve(TableOperationalStatus.Active, hasOpenSession: false, hasReservationInWindow: false));
    }

    [Fact]
    public void Reservation_makes_table_reserved_only_when_confirmed_and_in_window()
    {
        var r = Reservation.Create(1, 1, 1, Now.AddMinutes(10), 2);
        Assert.False(r.MakesTableReservedAt(Now, Window)); // PENDING

        r.Confirm();
        // reservation_time = Now + 10 min; ventana 30/30 => reservado en [Now-20, Now+40].
        Assert.True(r.MakesTableReservedAt(Now, Window));
        Assert.True(r.MakesTableReservedAt(Now.AddMinutes(-20), Window));  // borde inferior
        Assert.False(r.MakesTableReservedAt(Now.AddMinutes(-21), Window)); // fuera por delante
        Assert.True(r.MakesTableReservedAt(Now.AddMinutes(40), Window));   // borde superior
        Assert.False(r.MakesTableReservedAt(Now.AddMinutes(41), Window));  // fuera por detrás
    }
}

public class TableTests
{
    [Fact]
    public void New_table_rejects_non_positive_number_or_capacity()
    {
        Assert.Throws<DomainRuleException>(() => new Table(1, 0, 4));
        Assert.Throws<DomainRuleException>(() => new Table(1, 3, 0));
    }

    [Fact]
    public void Cannot_set_cleaning_or_out_of_service_with_open_session()
    {
        var table = new Table(1, 1, 4);
        var ex = Assert.Throws<DomainRuleException>(() =>
            table.SetOperationalStatus(TableOperationalStatus.Cleaning, hasOpenSession: true));
        Assert.Equal("dining.table_busy", ex.Code);

        Assert.Throws<DomainRuleException>(() =>
            table.SetOperationalStatus(TableOperationalStatus.OutOfService, hasOpenSession: true));
    }

    [Fact]
    public void Can_return_to_active_with_open_session_and_can_change_when_free()
    {
        var table = new Table(1, 1, 4);
        table.SetOperationalStatus(TableOperationalStatus.Active, hasOpenSession: true);
        Assert.Equal(TableOperationalStatus.Active, table.OperationalStatus);

        table.SetOperationalStatus(TableOperationalStatus.Cleaning, hasOpenSession: false);
        Assert.Equal(TableOperationalStatus.Cleaning, table.OperationalStatus);
    }
}

public class TableSessionTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 20, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Open_rejects_non_positive_guest_count()
    {
        Assert.Throws<DomainRuleException>(() => TableSession.Open(1, 0, Now));
        Assert.Throws<DomainRuleException>(() => TableSession.Open(1, -3, Now));
    }

    [Fact]
    public void Close_rejects_time_before_open_and_double_close()
    {
        var s = TableSession.Open(1, 2, Now);

        var early = Assert.Throws<DomainRuleException>(() => s.Close(Now.AddMinutes(-1)));
        Assert.Equal("dining.close_before_open", early.Code);

        s.Close(Now.AddHours(1));
        Assert.False(s.IsOpen);
        Assert.Throws<DomainRuleException>(() => s.Close(Now.AddHours(2)));
    }
}

public class ReservationTests
{
    [Fact]
    public void Create_rejects_non_positive_party_size()
        => Assert.Throws<DomainRuleException>(() => Reservation.Create(1, 1, 1, DateTime.Now, 0));

    [Fact]
    public void Confirm_only_from_pending()
    {
        var r = Reservation.Create(1, 1, 1, DateTime.Now, 2);
        r.Confirm();
        var ex = Assert.Throws<DomainRuleException>(() => r.Confirm());
        Assert.Equal("dining.reservation_invalid_transition", ex.Code);
    }

    [Fact]
    public void Cancel_not_allowed_after_seated()
    {
        var r = Reservation.Create(1, 1, 1, DateTime.Now, 2);
        r.Confirm();
        r.MarkSeated();
        Assert.Throws<DomainRuleException>(() => r.Cancel());
    }

    [Fact]
    public void NoShow_only_from_confirmed()
    {
        var r = Reservation.Create(1, 1, 1, DateTime.Now, 2);
        Assert.Throws<DomainRuleException>(() => r.MarkNoShow()); // PENDING
        r.Confirm();
        r.MarkNoShow();
        Assert.Equal(ReservationStatus.NoShow, r.Status);
    }

    [Fact]
    public void Seat_only_from_confirmed()
    {
        var r = Reservation.Create(1, 1, 1, DateTime.Now, 2);
        Assert.Throws<DomainRuleException>(() => r.MarkSeated());
        r.Confirm();
        r.MarkSeated();
        Assert.Equal(ReservationStatus.Seated, r.Status);
    }
}
