using TicketGuard.Domain.Events;

namespace TicketGuard.UnitTests.Events;

public class EventIsOnSaleTests
{
    private static readonly DateTimeOffset StartsAt = new(2026, 12, 1, 20, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SaleStartsAt = StartsAt.AddDays(-30);

    private static Event CreateEvent(EventStatus status = EventStatus.Published) => new()
    {
        Name = "Test Show",
        Artist = "Test Artist",
        Status = status,
        StartsAt = StartsAt,
        SaleStartsAt = SaleStartsAt
    };

    [Fact]
    public void IsOnSale_DuringSalePeriod_ReturnsTrue()
    {
        var evt = CreateEvent();

        Assert.True(evt.IsOnSale(SaleStartsAt.AddDays(1)));
    }

    [Fact]
    public void IsOnSale_BeforeSaleStarts_ReturnsFalse()
    {
        var evt = CreateEvent();

        Assert.False(evt.IsOnSale(SaleStartsAt.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(-11, true)]  // 11 minutes before start: still on sale
    [InlineData(-10, false)] // exactly 10 minutes before: sales are closed
    [InlineData(-5, false)]
    [InlineData(30, false)]  // after the event started
    public void IsOnSale_NearEventStart_ClosesTenMinutesBefore(int minutesFromStart, bool expected)
    {
        var evt = CreateEvent();

        Assert.Equal(expected, evt.IsOnSale(StartsAt.AddMinutes(minutesFromStart)));
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.Cancelled)]
    public void IsOnSale_WhenNotPublished_ReturnsFalse(EventStatus status)
    {
        var evt = CreateEvent(status);

        Assert.False(evt.IsOnSale(SaleStartsAt.AddDays(1)));
    }
}
