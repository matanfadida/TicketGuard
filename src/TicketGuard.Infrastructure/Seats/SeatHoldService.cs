using Microsoft.EntityFrameworkCore;
using TicketGuard.Application.Seats;
using TicketGuard.Domain.Events;
using TicketGuard.Infrastructure.Persistence;

namespace TicketGuard.Infrastructure.Seats;

public class SeatHoldService(TicketGuardDbContext db, TimeProvider timeProvider) : ISeatHoldService
{
    public async Task<SeatHoldResult> HoldAsync(int eventId, int seatId, Guid holderId, CancellationToken cancellationToken = default)
    {
        var evt = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (evt is null)
        {
            return new SeatHoldResult(SeatHoldStatus.NotFound);
        }

        var now = timeProvider.GetUtcNow();

        if (!evt.IsOnSale(now))
        {
            return new SeatHoldResult(SeatHoldStatus.EventNotOnSale);
        }

        // TODO (step 3): the deadline will come from the buyer's queue entry, not from the event directly.
        var heldUntil = now.AddMinutes(evt.SelectionMinutesWithQueue);

        // The core of the system: one atomic UPDATE.
        // The condition is checked by SQL Server inside the UPDATE, while the row is locked,
        // so two buyers can never both get the seat, even from different API servers.
        // A seat can be taken if it is Available, or Held by someone whose time ran out.
        var updatedRows = await db.EventSeats
            .Where(s => s.EventId == eventId && s.SeatId == seatId &&
                        (s.Status == EventSeatStatus.Available ||
                         (s.Status == EventSeatStatus.Held && s.HeldUntil < now)))
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.Status, EventSeatStatus.Held)
                .SetProperty(s => s.HeldByQueueEntryId, holderId)
                .SetProperty(s => s.HeldUntil, heldUntil),
                cancellationToken);

        if (updatedRows == 1)
        {
            return new SeatHoldResult(SeatHoldStatus.Held, heldUntil);
        }

        // 0 rows: either the seat is taken, or it does not exist in this event. Tell them apart for a clear response.
        var seatExists = await db.EventSeats.AnyAsync(s => s.EventId == eventId && s.SeatId == seatId, cancellationToken);

        return new SeatHoldResult(seatExists ? SeatHoldStatus.Unavailable : SeatHoldStatus.NotFound);
    }

    public async Task<bool> ReleaseAsync(int eventId, int seatId, Guid holderId, CancellationToken cancellationToken = default)
    {
        // Same idea: one atomic UPDATE, and only the buyer who holds the seat can release it.
        var updatedRows = await db.EventSeats
            .Where(s => s.EventId == eventId && s.SeatId == seatId &&
                        s.Status == EventSeatStatus.Held && s.HeldByQueueEntryId == holderId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.Status, EventSeatStatus.Available)
                .SetProperty(s => s.HeldByQueueEntryId, (Guid?)null)
                .SetProperty(s => s.HeldUntil, (DateTimeOffset?)null),
                cancellationToken);

        return updatedRows == 1;
    }
}
