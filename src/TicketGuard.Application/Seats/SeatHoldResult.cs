namespace TicketGuard.Application.Seats;

public enum SeatHoldStatus
{
    /// <summary>The seat is now held by this buyer.</summary>
    Held,

    /// <summary>Someone else holds or bought the seat.</summary>
    Unavailable,

    /// <summary>The event is not on sale (draft, cancelled, not open yet, or starting within 10 minutes).</summary>
    EventNotOnSale,

    /// <summary>The event or the seat does not exist.</summary>
    NotFound
}

public record SeatHoldResult(SeatHoldStatus Status, DateTimeOffset? HeldUntil = null);
