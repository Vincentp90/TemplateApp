using Application;
using DataAccess.Auctions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Dynamic;
using System.Security.Claims;
using WishlistApi.DTOs;
using WishlistApi.Helpers;

namespace WishlistApi.Controllers
{
    public class AuctionHub : Hub
    {
    }

    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class AuctionsController(IUserContext _userContext, IAuctionService _auctionService, IHubContext<AuctionHub> _hub) : ControllerBase
    {
        [HttpGet("current")]
        public async Task<ActionResult<AuctionDTOs.Auction>> GetCurrentAuctionAsync()
        {
            var auction = await _auctionService.GetLatestAuctionAsync();
            if(auction == null)
                return NoContent();

            return Ok(new AuctionDTOs.Auction(
                ID: auction.ID,
                StartDate: auction.DateAdded,
                EndDate: auction.DateAdded + Auction.Duration,
                UserHasBid: (auction.User?.UUID.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier)),
                StartingPrice: auction.StartingPrice,
                CurrentPrice: auction.CurrentPrice,
                AppID: auction.appid,
                AppName: auction.AppListing.name,
                RowVersion: auction.RowVersion
            ));
        }

        [HttpPost("current")]
        public async Task<ActionResult> PostAuctionAsync(AuctionDTOs.Auction auction)
        {
            int internalUserId = await _userContext.GetIdAsync();

            try
            {
                await _auctionService.UpdateAuctionBidAsync(new Auction { 
                    ID = auction.ID, 
                    CurrentPrice = auction.CurrentPrice, 
                    RowVersion = auction.RowVersion,
                    UserID = internalUserId
                });
                _ = _hub.Clients.All.SendAsync("AuctionUpdated");
                return Ok();
            }
            catch(DbUpdateConcurrencyException)
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
                await _auctionService.SimulateBid();
                _ = _hub.Clients.All.SendAsync("AuctionUpdated");
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
        }
    }
}
