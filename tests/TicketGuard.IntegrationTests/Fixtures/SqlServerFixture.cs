using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TicketGuard.Domain.Events;
using TicketGuard.Domain.Venues;
using TicketGuard.Infrastructure.Persistence;

namespace TicketGuard.IntegrationTests.Fixtures;

/// <summary>
/// Shared setup for tests that need a real database.
/// Runs once per test class: starts SQL Server in a throwaway container, applies the migrations,
/// and starts the API in memory, connected to that container. Everything is deleted at the end.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private const string ConnectionStringVariable = "ConnectionStrings__SqlServer";

    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Pulls the image (first time only) and starts SQL Server. Takes ~10-30 seconds.
        await _container.StartAsync();

        // Environment variables override appsettings.json and User Secrets,
        // so the API under test talks to the container and never to your local database.
        Environment.SetEnvironmentVariable(ConnectionStringVariable, _container.GetConnectionString());

        Factory = new WebApplicationFactory<Program>();

        // Create the tables in the container from the same migrations as production.
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketGuardDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        Environment.SetEnvironmentVariable(ConnectionStringVariable, null);
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a venue with one seat and an event that is on sale right now.
    /// Each test calls this to get its own fresh seat, so tests never affect each other.
    /// </summary>
    public async Task<(int EventId, int SeatId)> CreateEventWithOneSeatAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketGuardDbContext>();
        var now = DateTimeOffset.UtcNow;

        var seat = new Seat { Row = "A", Number = 1 };
        var venue = new Venue
        {
            Name = "Test Arena",
            City = "Tel Aviv",
            Address = "1 Test St",
            StagePosition = "Top",
            Sections = [new Section { Name = "Orchestra", Seats = [seat] }]
        };

        var evt = new Event
        {
            Name = "Test Show",
            Artist = "Test Artist",
            Venue = venue,
            Status = EventStatus.Published,
            SaleStartsAt = now.AddDays(-1),
            WaitingRoomOpensAt = now.AddDays(-1),
            StartsAt = now.AddDays(30),
            MaxActiveBuyers = 1000,
            SelectionMinutesWithQueue = 10,
            SelectionMinutesWithoutQueue = 30,
            PaymentExtensionMinutes = 5,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Events.Add(evt);
        await db.SaveChangesAsync(); // Saves the venue, section and seat too, and fills in their Ids.

        db.EventSeats.Add(new EventSeat { EventId = evt.Id, SeatId = seat.Id });
        await db.SaveChangesAsync();

        return (evt.Id, seat.Id);
    }
}
