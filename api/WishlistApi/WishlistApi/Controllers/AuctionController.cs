using DataAccess.Auctions;
using DataAccess.Users;
using DataAccess.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Dynamic;
using System.Security.Claims;
using WishlistApi.DTOs;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class AuctionController : ControllerBase
    {
        private readonly IAuctionDA _auctionDA;
        private readonly IUserDA _userDA;

        public AuctionController(IAuctionDA auctionDA, IUserDA userDA)
        {
            _auctionDA = auctionDA;
            _userDA = userDA;
        }

        [HttpGet()]
        public async Task<ActionResult<AuctionDTOs.Auction>> GetCurrentAuctionAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            var auction = await _auctionDA.GetLatestAuctionAsync();
            if(auction == null)
                return NoContent();

            return Ok(new AuctionDTOs.Auction(
                ID: auction.ID,
                StartDate: auction.DateAdded,
                EndDate: auction.DateAdded + Auction.Duration,
                UserHasBid: (auction.User?.UUID.ToString() == userId),
                StartingPrice: auction.StartingPrice,
                CurrentPrice: auction.CurrentPrice,
                AppName: auction.AppListing.name,
                RowVersion: auction.RowVersion
            ));
        }

        [HttpPost()]
        public async Task<ActionResult> PostAuctionAsync(AuctionDTOs.Auction auction)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));

            try
            {
                await _auctionDA.UpdateAuctionAsync(new Auction { 
                    ID = auction.ID, 
                    CurrentPrice = auction.CurrentPrice, 
                    RowVersion = auction.RowVersion,
                    UserID = internalUserId
                });
                return Ok();
            }
            catch(DBConcurrencyException ex)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
        }
    }
}
