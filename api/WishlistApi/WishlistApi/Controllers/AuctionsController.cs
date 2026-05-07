using DataAccess.Auctions;
using DataAccess.Users;
using DataAccess.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
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
    public class AuctionsController : ControllerBase
    {
        private readonly IUserContext _userContext;
        private readonly IUserDA _userDA;
        private readonly IAuctionDA _auctionDA;        
        private readonly IHubContext<AuctionHub> _hub;
        private readonly IConfiguration _config;

        public AuctionsController(IUserContext userContext, IAuctionDA auctionDA, IUserDA userDA, IHubContext<AuctionHub> hub, IConfiguration config)
        {
            _userContext = userContext;
            _auctionDA = auctionDA;
            _userDA = userDA;
            _hub = hub;
            _config = config;
        }

        [HttpGet("current")]
        public async Task<ActionResult<AuctionDTOs.Auction>> GetCurrentAuctionAsync()
        {
            var auction = await _auctionDA.GetLatestAuctionAsync();
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
                await _auctionDA.UpdateAuctionBidAsync(new Auction { 
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

        [HttpGet("current/SimulateBid")]
        public async Task<ActionResult> PostSimulateBidAsync()
        {
            var user = await GetSimulationUser();

            try
            {
                Auction auction = (await _auctionDA.GetLatestAuctionAsync())!;
                var newPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
                auction.CurrentPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
                auction.UserID = user.ID;
                await _auctionDA.UpdateAuctionBidAsync(auction);
                _ = _hub.Clients.All.SendAsync("AuctionUpdated");
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
        }

        private async Task<User> GetSimulationUser()
        {
            const string username = "SimulateAuctionUser";
            string password = _config["SimUserPassword"]!;
            var user = await _userDA.LoginUserAsync(username, password);
            if (user == null)
            {
                await _userDA.AddUserAsync(username, password);
                user = await _userDA.LoginUserAsync(username, password);
            }
            return user!;
        }
    }
}
