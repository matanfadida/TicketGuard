namespace TicketGuard.Domain.Venues;

/// <summary>A fixed seat in a venue, created once with the venue. Its state per event lives in EventSeat.</summary>
public class Seat
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public required string Row { get; set; }
    public int Number { get; set; }

    /// <summary>Position on the seat map, used to draw it in the frontend.</summary>
    public int MapX { get; set; }
    public int MapY { get; set; }

    public Section Section { get; set; } = null!;
}
