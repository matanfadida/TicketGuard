using TicketGuard.Domain.Venues;

namespace TicketGuard.Domain.Events;

/// <summary>
/// The state of one seat in one event: the link between an event and a seat.
/// Holding a seat is an atomic update on this row:
/// "set it to Held only if it is still Available", so two buyers can never hold the same seat.
/// </summary>
public class EventSeat
{
    public long Id { get; set; }
    public int EventId { get; set; }
    public int SeatId { get; set; }

    public EventSeatStatus Status { get; set; } = EventSeatStatus.Available;

    /// <summary>
    /// The queue entry holding this seat. The QueueEntry entity comes in step 3 (queue in Redis),
    /// so for now this is a plain value with no foreign key.
    /// </summary>
    public Guid? HeldByQueueEntryId { get; set; }

    public DateTimeOffset? HeldUntil { get; set; }

    public Event Event { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}
