namespace TicketGuard.Application.Seats;

/// <summary>
/// Holding and releasing seats during checkout.
/// Defined here in Application, implemented in Infrastructure (it needs the database),
/// so the business flow does not depend on EF Core directly.
/// </summary>
public interface ISeatHoldService
{
    /// <summary>
    /// Tries to hold a seat for a buyer. Safe under concurrency:
    /// if many buyers try the same seat at the same moment, exactly one gets it.
    /// </summary>
    Task<SeatHoldResult> HoldAsync(int eventId, int seatId, Guid holderId, CancellationToken cancellationToken = default);

    /// <summary>Releases a seat, only if this buyer is the one holding it.</summary>
    Task<bool> ReleaseAsync(int eventId, int seatId, Guid holderId, CancellationToken cancellationToken = default);
}
