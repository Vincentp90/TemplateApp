using Application;
using Application.Commands;
using Application.Contracts;
using Application.Queries;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Dynamic;
using System.Security.Claims;
using WishlistApi.Helpers;

namespace WishlistApi.Controllers
{
    public class AuctionHub : Hub
    {
    }

    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class AuctionsController(IUserContext userContext, IAuctionService auctionService, IAuctionQueries auctionQueries, IHubContext<AuctionHub> hub) : ControllerBase
    {
        [HttpGet("current")]
        public async Task<ActionResult<AuctionDto>> GetCurrentAuctionAsync()
        {
            var userClaimNameId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? userGuid = userClaimNameId == null ? null : new Guid(userClaimNameId);
            var auction = await auctionQueries.GetCurrentAuctionAsync(userGuid);
            if(auction == null)
                return NoContent();

            return Ok(auction);
        }

        [HttpPost("current")]
        public async Task<ActionResult> PostAuctionAsync(AuctionDto auction)
        {
            int internalUserId = await userContext.GetIdAsync();

            if(auction.CurrentPrice == null)
                return BadRequest("CurrentPrice is required");

            try
            {
                await auctionService.PlaceBidAsync(new PlaceBidCommand(
                    AuctionId: auction.ID, 
                    Amount: auction.CurrentPrice.Value, 
                    RowVersion: auction.RowVersion,
                    UserId: internalUserId
                ));
                _ = hub.Clients.All.SendAsync("AuctionUpdated");
                return Ok();
            }
            catch (DomainException)
            {
                // We could later do something more intelligent with this instead of handling it the same as a concurrency issue
                return StatusCode(StatusCodes.Status409Conflict);
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
        }

        /// <summary>
        /// Simulate another user bidding, to more easily demonstrate optimistic concurrency
        /// </summary>
        /// <returns>Ok(200)</returns>
        [HttpGet("current/SimulateBid")]
        public async Task<ActionResult> PostSimulateBidAsync()
        {
            try
            {
                await auctionService.SimulateBid();
                _ = hub.Clients.All.SendAsync("AuctionUpdated");
                return Ok();
            }
            catch (DomainException)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
        }
    }
}
