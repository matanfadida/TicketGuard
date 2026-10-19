namespace TicketGuard.Domain.Venues;

/// <summary>An area inside a venue, e.g. "Orchestra", "Balcony" or "VIP". Prices are set per section per event.</summary>
public class Section
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public required string Name { get; set; }

    public Venue Venue { get; set; } = null!;
    public List<Seat> Seats { get; set; } = [];
}
