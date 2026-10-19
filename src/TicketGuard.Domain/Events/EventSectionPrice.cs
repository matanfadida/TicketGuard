using TicketGuard.Domain.Venues;

namespace TicketGuard.Domain.Events;

/// <summary>The price of a section in a specific event. Links an event and a section.</summary>
public class EventSectionPrice
{
    public int EventId { get; set; }
    public int SectionId { get; set; }
    public decimal Price { get; set; }

    public Event Event { get; set; } = null!;
    public Section Section { get; set; } = null!;
}
