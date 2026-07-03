using Application;
using Application.Commands;
using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WishlistApi.Helpers;
using System.Globalization;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IUserContext _userContext;
        private readonly IWishlistService _wishlistService;
        private readonly ISharedDbPriceReader _priceReader;
        private readonly ISteamTrackerAlertProxy _alertProxy;

        public WishlistController(IUserContext userContext, IWishlistService wishlistService, ISharedDbPriceReader priceReader, ISteamTrackerAlertProxy alertProxy)
        {
            _userContext = userContext;
            _wishlistService = wishlistService;
            _priceReader = priceReader;
            _alertProxy = alertProxy;
        }

        [HttpGet()]
        public async Task<ActionResult<Wishlist>> GetWishlistAsync([FromQuery] string? fields = null)// TODO replace fields filtering with OData
        {
            int internalUserId = await _userContext.GetIdAsync();

            var fieldList = (fields ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(f => f.ToLower())
                                .ToHashSet();
            bool includeAll = fieldList.Count == 0;

            bool Has(string field) => includeAll || fieldList.Contains(field);

            // 1. Get local wishlist items
            var localItems = await _wishlistService.GetWishlistItemsAsync(internalUserId);
            var appIds = localItems.Select(x => x.AppId).ToList();

            // 2. Read prices from shared DB (SteamTracker)
            var prices = await _priceReader.GetPricesAsync(appIds);

            // 3. Read alert rules from shared DB (SteamTracker)
            var alertRules = await _priceReader.GetAlertRulesAsync(internalUserId.ToString());

            // 4. Merge everything
            var result = localItems.Select(x => new WishlistItemDto(
                AppId: Has("appid") ? x.AppId : null,
                DateAdded: Has("dateadded") ? x.DateAdded : null,
                Name: Has("name") ? x.AppName : null,
                Price: prices.TryGetValue(x.AppId, out var price) && Has("price") ? price.Amount : null,
                PriceCurrency: prices.TryGetValue(x.AppId, out price) && Has("pricecurrency") ? price.Currency : (string?)"EUR",
                LastCheckedAt: prices.TryGetValue(x.AppId, out price) && Has("lastcheckedat") ? price.LastCheckedAt : null,
                AlertRuleId: alertRules.TryGetValue(x.AppId, out var alert) && Has("alertruleid") ? alert.Id : null,
                AlertThreshold: alertRules.TryGetValue(x.AppId, out alert) && Has("alertthreshold") ? alert.ThresholdAmount : null,
                AlertCurrency: alertRules.TryGetValue(x.AppId, out alert) && Has("alertcurrency") ? alert.Currency : (string?)"EUR"
            ));

            return Ok(new Wishlist(result));
        }

        [HttpGet("stats")]
        public async Task<ActionResult<Stats>> GetWishlistStatsAsync()
        {
            int internalUserId = await _userContext.GetIdAsync();

            var stats = await _wishlistService.GetWishlistStatsAsync(internalUserId);
            return Ok(new Stats(
                AvgTimeAdded: stats.AvgTimeAdded,
                AvgTimeBetweenAdded: stats.AvgTimeBetweenAdded,
                OldestItem: stats.OldestItem,
                MostCommonCharacter: stats.MostCommonCharacter
                ));
        }

        // TODO route doesn't make that much sense, /wishlist/apps/{appId} would be better
        [HttpPost("{appId}")]
        public async Task<ActionResult> AddWishlistItemAsync(int appId)
        {
            int internalUserId = await _userContext.GetIdAsync();
            try
            {
                await _wishlistService.AddToWishlistAsync(new AddToWishlistCommand(UserId: internalUserId, AppId: appId));
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return StatusCode(StatusCodes.Status409Conflict, ex.Message);
            }
            return Ok();
        }

        // TODO route, see above
        [HttpDelete("{appId}")]
        public async Task<ActionResult> DeleteAppFromWishlistAsync(int appId)
        {
            int internalUserId = await _userContext.GetIdAsync();
            await _wishlistService.DeleteWishlistItemAsync(internalUserId, appId);
            return Ok();
        }

        /// <summary>
        /// Backfill: publishes WishlistItemAdded events for all wishlist items.
        /// Used to sync SteamTracker when the service is deployed after users already have items.
        /// </summary>
        [HttpPost("_backfill")]
        public async Task<ActionResult> BackfillAsync()
        {
            int internalUserId = await _userContext.GetIdAsync();

            var items = await _wishlistService.GetWishlistItemsAsync(internalUserId);

            if (!items.Any())
            {
                return new ObjectResult(new { Count = 0 }) { StatusCode = 202 };
            }

            foreach (var item in items)
            {
                await _wishlistService.PublishBackfillEventAsync(item);
            }

            return new ObjectResult(new { Count = items.Count }) { StatusCode = 202 };
        }

        /// <summary>
        /// Proxy: sets an alert rule for a wishlisted game (delegates to SteamTracker).
        /// </summary>
        [HttpPost("{appId}/alert")]
        public async Task<ActionResult> SetAlertAsync(int appId, [FromQuery] decimal thresholdAmount, [FromQuery] string currency = "EUR")
        {
            int internalUserId = await _userContext.GetIdAsync();
            await _alertProxy.SetAlertRuleAsync(internalUserId.ToString(), appId, thresholdAmount, currency);
            return Ok();
        }

        /// <summary>
        /// Proxy: deletes an alert rule (delegates to SteamTracker).
        /// </summary>
        [HttpDelete("{alertRuleId}/alert")]
        public async Task<ActionResult> DeleteAlertAsync(Guid alertRuleId)
        {
            int internalUserId = await _userContext.GetIdAsync();
            await _alertProxy.DeleteAlertRuleAsync(internalUserId.ToString(), alertRuleId);
            return Ok();
        }
    }
}
