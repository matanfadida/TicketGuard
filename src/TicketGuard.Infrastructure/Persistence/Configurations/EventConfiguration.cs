using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGuard.Domain.Events;

namespace TicketGuard.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Artist).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.ImageUrl).HasMaxLength(500);

        // Stored as text ("Published") instead of a number (1), so the table is readable in SSMS.
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        // Restrict: a venue that has events cannot be deleted.
        // This also avoids "multiple cascade paths", which SQL Server does not allow.
        builder.HasOne(e => e.Venue)
            .WithMany()
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.SectionPrices)
            .WithOne(p => p.Event)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Seats)
            .WithOne(s => s.Event)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventSectionPriceConfiguration : IEntityTypeConfiguration<EventSectionPrice>
{
    public void Configure(EntityTypeBuilder<EventSectionPrice> builder)
    {
        // Composite key: one price per section per event.
        builder.HasKey(p => new { p.EventId, p.SectionId });

        builder.Property(p => p.Price).HasPrecision(10, 2);

        builder.HasOne(p => p.Section)
            .WithMany()
            .HasForeignKey(p => p.SectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventSeatConfiguration : IEntityTypeConfiguration<EventSeat>
{
    public void Configure(EntityTypeBuilder<EventSeat> builder)
    {
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // Each seat appears exactly once per event.
        builder.HasIndex(s => new { s.EventId, s.SeatId }).IsUnique();

        // Fast lookup of "all available seats in this event" for the seat map.
        builder.HasIndex(s => new { s.EventId, s.Status });

        builder.HasOne(s => s.Seat)
            .WithMany()
            .HasForeignKey(s => s.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
