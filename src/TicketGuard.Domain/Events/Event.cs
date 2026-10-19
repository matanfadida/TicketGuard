using TicketGuard.Domain.Venues;

namespace TicketGuard.Domain.Events;

public class Event
{
    /// <summary>Sales close this long before the event starts.</summary>
    public static readonly TimeSpan SalesCloseBeforeStart = TimeSpan.FromMinutes(10);

    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Artist { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public int VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset SaleStartsAt { get; set; }
    public DateTimeOffset WaitingRoomOpensAt { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    // Sale settings, set by the admin per event (see README section 3).

    /// <summary>X: how many buyers can be in seat selection at the same time.</summary>
    public int MaxActiveBuyers { get; set; }

    /// <summary>Y: minutes to choose seats when buyers are waiting in the queue.</summary>
    public int SelectionMinutesWithQueue { get; set; }

    /// <summary>Y2: minutes to choose seats when nobody is waiting.</summary>
    public int SelectionMinutesWithoutQueue { get; set; }

    /// <summary>K: minutes added when the buyer reaches payment.</summary>
    public int PaymentExtensionMinutes { get; set; }

    /// <summary>Z: tickets per buyer. Null means no limit.</summary>
    public int? MaxTicketsPerBuyer { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<EventSectionPrice> SectionPrices { get; set; } = [];
    public List<EventSeat> Seats { get; set; } = [];

    /// <summary>
    /// Business rule: tickets can be sold only for a published event, after the sale opens,
    /// and until 10 minutes before the event starts.
    /// </summary>
    public bool IsOnSale(DateTimeOffset now) =>
        Status == EventStatus.Published
        && now >= SaleStartsAt
        && now < StartsAt - SalesCloseBeforeStart;
}
