using Microsoft.EntityFrameworkCore;
using TicketGuard.Domain.Events;
using TicketGuard.Domain.Venues;

namespace TicketGuard.Infrastructure.Persistence;

public class TicketGuardDbContext(DbContextOptions<TicketGuardDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventSectionPrice> EventSectionPrices => Set<EventSectionPrice>();
    public DbSet<EventSeat> EventSeats => Set<EventSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration class in this project (the Configurations folder).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketGuardDbContext).Assembly);
    }
}
