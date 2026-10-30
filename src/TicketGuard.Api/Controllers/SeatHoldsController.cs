using Microsoft.AspNetCore.Mvc;
using TicketGuard.Application.Seats;

namespace TicketGuard.Api.Controllers;

[ApiController]
[Route("events/{eventId:int}/seats/{seatId:int}/hold")]
public class SeatHoldsController(ISeatHoldService seatHoldService) : ControllerBase
{
    // TEMPORARY until step 3: the buyer identifies with an X-Holder-Id header.
    // In step 3 this becomes the signed queue token, so nobody can hold seats without passing the queue.
    private const string HolderHeader = "X-Holder-Id";

    [HttpPost]
    public async Task<IActionResult> Hold(int eventId, int seatId, [FromHeader(Name = HolderHeader)] Guid holderId, CancellationToken cancellationToken)
    {
        if (holderId == Guid.Empty)
        {
            return BadRequest(new { error = $"Missing {HolderHeader} header" });
        }

        var result = await seatHoldService.HoldAsync(eventId, seatId, holderId, cancellationToken);

        return result.Status switch
        {
            SeatHoldStatus.Held => Ok(new { heldUntil = result.HeldUntil }),
            SeatHoldStatus.Unavailable => Conflict(new { error = "Seat is not available" }),
            SeatHoldStatus.EventNotOnSale => Conflict(new { error = "Event is not on sale" }),
            _ => NotFound()
        };
    }

    [HttpDelete]
    public async Task<IActionResult> Release(int eventId, int seatId, [FromHeader(Name = HolderHeader)] Guid holderId, CancellationToken cancellationToken)
    {
        if (holderId == Guid.Empty)
        {
            return BadRequest(new { error = $"Missing {HolderHeader} header" });
        }

        var released = await seatHoldService.ReleaseAsync(eventId, seatId, holderId, cancellationToken);

        return released ? NoContent() : NotFound();
    }
}
