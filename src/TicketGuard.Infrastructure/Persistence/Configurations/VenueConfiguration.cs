using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGuard.Domain.Venues;

namespace TicketGuard.Infrastructure.Persistence.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.Property(v => v.Name).HasMaxLength(200);
        builder.Property(v => v.City).HasMaxLength(100);
        builder.Property(v => v.Address).HasMaxLength(300);
        builder.Property(v => v.PostalCode).HasMaxLength(20);
        builder.Property(v => v.StagePosition).HasMaxLength(50);

        // Deleting a venue deletes its sections, and their seats.
        builder.HasMany(v => v.Sections)
            .WithOne(s => s.Venue)
            .HasForeignKey(s => s.VenueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(100);

        // Two sections in the same venue cannot have the same name.
        builder.HasIndex(s => new { s.VenueId, s.Name }).IsUnique();

        builder.HasMany(s => s.Seats)
            .WithOne(seat => seat.Section)
            .HasForeignKey(seat => seat.SectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.Property(s => s.Row).HasMaxLength(10);

        // The same row and number cannot appear twice in a section.
        builder.HasIndex(s => new { s.SectionId, s.Row, s.Number }).IsUnique();
    }
}
