using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketGuard.Domain.Events;
using TicketGuard.Infrastructure.Persistence;
using TicketGuard.IntegrationTests.Fixtures;

namespace TicketGuard.IntegrationTests.Seats;

public class SeatHoldTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Hold_WhenManyBuyersTryTheSameSeatAtOnce_ExactlyOneSucceeds()
    {
        // Arrange: one seat, and 100 different buyers.
        var (eventId, seatId) = await fixture.CreateEventWithOneSeatAsync();
        var buyers = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        // Act: all 100 send their request at the same time.
        var requests = buyers.Select(buyerId => HoldAsync(eventId, seatId, buyerId));
        var responses = await Task.WhenAll(requests);

        // Assert: exactly one got the seat, everyone else was told it is taken.
        var winners = responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToList();
        Assert.Single(winners);
        Assert.Equal(99, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        // And the database agrees: the seat is held by the one buyer who got 200.
        var winnerIndex = Array.IndexOf(responses, winners[0]);
        var seat = await GetEventSeatAsync(eventId, seatId);
        Assert.Equal(EventSeatStatus.Held, seat.Status);
        Assert.Equal(buyers[winnerIndex], seat.HeldByQueueEntryId);
    }

    [Fact]
    public async Task Hold_WhenPreviousHoldExpired_Succeeds()
    {
        var (eventId, seatId) = await fixture.CreateEventWithOneSeatAsync();
        var firstBuyer = Guid.NewGuid();
        var secondBuyer = Guid.NewGuid();
        await HoldAsync(eventId, seatId, firstBuyer);

        // Simulate the first buyer's time running out, without waiting 10 minutes.
        await ExpireHoldAsync(eventId, seatId);

        var response = await HoldAsync(eventId, seatId, secondBuyer);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var seat = await GetEventSeatAsync(eventId, seatId);
        Assert.Equal(secondBuyer, seat.HeldByQueueEntryId);
    }

    [Fact]
    public async Task Hold_WhenSeatHeldByAnotherBuyer_ReturnsConflict()
    {
        var (eventId, seatId) = await fixture.CreateEventWithOneSeatAsync();
        await HoldAsync(eventId, seatId, Guid.NewGuid());

        var response = await HoldAsync(eventId, seatId, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Release_ByAnotherBuyer_DoesNotReleaseTheSeat()
    {
        var (eventId, seatId) = await fixture.CreateEventWithOneSeatAsync();
        var owner = Guid.NewGuid();
        await HoldAsync(eventId, seatId, owner);

        var response = await ReleaseAsync(eventId, seatId, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var seat = await GetEventSeatAsync(eventId, seatId);
        Assert.Equal(owner, seat.HeldByQueueEntryId);
    }

    [Fact]
    public async Task Release_ByTheOwner_MakesTheSeatAvailableAgain()
    {
        var (eventId, seatId) = await fixture.CreateEventWithOneSeatAsync();
        var owner = Guid.NewGuid();
        await HoldAsync(eventId, seatId, owner);

        var response = await ReleaseAsync(eventId, seatId, owner);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var seat = await GetEventSeatAsync(eventId, seatId);
        Assert.Equal(EventSeatStatus.Available, seat.Status);
        Assert.Null(seat.HeldByQueueEntryId);
    }

    // ----- helpers -----

    private Task<HttpResponseMessage> HoldAsync(int eventId, int seatId, Guid buyerId) =>
        SendAsync(HttpMethod.Post, eventId, seatId, buyerId);

    private Task<HttpResponseMessage> ReleaseAsync(int eventId, int seatId, Guid buyerId) =>
        SendAsync(HttpMethod.Delete, eventId, seatId, buyerId);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, int eventId, int seatId, Guid buyerId)
    {
        var client = fixture.Factory.CreateClient();
        var request = new HttpRequestMessage(method, $"/events/{eventId}/seats/{seatId}/hold");
        request.Headers.Add("X-Holder-Id", buyerId.ToString());
        return await client.SendAsync(request);
    }

    private async Task<EventSeat> GetEventSeatAsync(int eventId, int seatId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketGuardDbContext>();
        return await db.EventSeats.AsNoTracking().SingleAsync(s => s.EventId == eventId && s.SeatId == seatId);
    }

    private async Task ExpireHoldAsync(int eventId, int seatId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketGuardDbContext>();
        await db.EventSeats
            .Where(s => s.EventId == eventId && s.SeatId == seatId)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.HeldUntil, DateTimeOffset.UtcNow.AddMinutes(-1)));
    }
}
