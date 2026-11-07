using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketGuard.Domain.Events;
using TicketGuard.Infrastructure.Persistence;

namespace TicketGuard.Infrastructure.Seats;

/// <summary>
/// Background job: every 30 seconds (now to 2 day beacuse the cost of azure) , returns seats whose hold expired to Available,
/// so the seat map shows them as free. Correctness does not depend on this job:
/// SeatHoldService already treats an expired hold as free.
/// </summary>
public class ExpiredHoldReleaser(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ExpiredHoldReleaser> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // A BackgroundService lives for the whole app, but a DbContext should be short-lived,
                // so each run creates its own scope and DbContext.
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TicketGuardDbContext>();
                var now = timeProvider.GetUtcNow();

                var released = await db.EventSeats
                    .Where(s => s.Status == EventSeatStatus.Held && s.HeldUntil < now)
                    .ExecuteUpdateAsync(set => set
                        .SetProperty(s => s.Status, EventSeatStatus.Available)
                        .SetProperty(s => s.HeldByQueueEntryId, (Guid?)null)
                        .SetProperty(s => s.HeldUntil, (DateTimeOffset?)null),
                        stoppingToken);

                if (released > 0)
                {
                    logger.LogInformation("Released {Count} expired seat holds", released);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one failed run (e.g. the database is briefly down) stop the whole app.
                logger.LogError(ex, "Failed to release expired seat holds");
            }
        }
    }
}
