namespace TicketGuard.Domain.Venues;

/// <summary>
/// A fixed venue, created once and reused for many events.
/// The number of seats is not stored: it is counted from the venue's seats.
/// </summary>
public class Venue
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string City { get; set; }
    public required string Address { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>Where the stage is on the seat map, e.g. "Top" or "Bottom".</summary>
    public required string StagePosition { get; set; }

    public List<Section> Sections { get; set; } = [];
}
