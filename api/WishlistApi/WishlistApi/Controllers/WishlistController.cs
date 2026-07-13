using Application;
using Application.Contracts;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain.Exceptions;
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
        private readonly IGetWishlistUseCase _getWishlistUseCase;
        private readonly IAddWishlistItemUseCase _addWishlistItemUseCase;
        private readonly IDeleteWishlistItemUseCase _deleteWishlistItemUseCase;
        private readonly IGetWishlistStatsUseCase _getWishlistStatsUseCase;
        private readonly IPublishBackfillEventUseCase _publishBackfillEventUseCase;
        private readonly ISetAlertRuleUseCase _setAlertRuleUseCase;
        private readonly IDeleteAlertRuleUseCase _deleteAlertRuleUseCase;
        private readonly ISharedDbPriceReader _priceReader;

        public WishlistController(
            IUserContext userContext,
            IGetWishlistUseCase getWishlistUseCase,
            IAddWishlistItemUseCase addWishlistItemUseCase,
            IDeleteWishlistItemUseCase deleteWishlistItemUseCase,
            IGetWishlistStatsUseCase getWishlistStatsUseCase,
            IPublishBackfillEventUseCase publishBackfillEventUseCase,
            ISetAlertRuleUseCase setAlertRuleUseCase,
            IDeleteAlertRuleUseCase deleteAlertRuleUseCase,
            ISharedDbPriceReader priceReader)
        {
            _userContext = userContext;
            _getWishlistUseCase = getWishlistUseCase;
            _addWishlistItemUseCase = addWishlistItemUseCase;
            _deleteWishlistItemUseCase = deleteWishlistItemUseCase;
            _getWishlistStatsUseCase = getWishlistStatsUseCase;
            _publishBackfillEventUseCase = publishBackfillEventUseCase;
            _setAlertRuleUseCase = setAlertRuleUseCase;
            _deleteAlertRuleUseCase = deleteAlertRuleUseCase;
            _priceReader = priceReader;
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
            var localItems = await _getWishlistUseCase.ExecuteAsync(new GetWishlistRequest(internalUserId));
            var appIds = localItems.Select(x => x.AppId).ToList();

            // 2. Read prices from shared DB (SteamTracker)
            var prices = await _priceReader.GetPricesAsync(appIds);

            // 3. Read alert rules from shared DB (SteamTracker)
            var alertRules = await _priceReader.GetAlertRulesAsync(internalUserId.ToString());

            // 4. Merge everything
            var result = localItems.Select(x => {
                var hasPrice = prices.TryGetValue(x.AppId, out var price);
                return new WishlistItemDto(
                    AppId: Has("appid") ? x.AppId : null,
                    DateAdded: Has("dateadded") ? x.DateAdded : null,
                    Name: Has("name") ? x.AppName : null,
                    Price: hasPrice && Has("price") ? price!.Amount : null,
                    PriceCurrency: hasPrice && Has("pricecurrency") ? price!.Currency : (string?)"EUR",
                    LastCheckedAt: hasPrice && Has("lastcheckedat") ? price!.LastCheckedAt : null,
                    IsUnavailable: hasPrice && price!.IsUnavailable,
                    AlertRuleId: alertRules.TryGetValue(x.AppId, out var alert) && Has("alertruleid") ? alert!.Id : null,
                    AlertThreshold: alertRules.TryGetValue(x.AppId, out alert) && Has("alertthreshold") ? alert!.ThresholdAmount : null,
                    AlertCurrency: alertRules.TryGetValue(x.AppId, out alert) && Has("alertcurrency") ? alert!.Currency : (string?)"EUR"
                );
            });

            return Ok(new Wishlist(result));
        }

        [HttpGet("stats")]
        public async Task<ActionResult<Stats>> GetWishlistStatsAsync()
        {
            int internalUserId = await _userContext.GetIdAsync();

            var stats = await _getWishlistStatsUseCase.ExecuteAsync(new GetWishlistStatsRequest(internalUserId));
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
                await _addWishlistItemUseCase.ExecuteAsync(new AddWishlistItemRequest(internalUserId, appId));
            }
            catch (DomainException ex)
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
            await _deleteWishlistItemUseCase.ExecuteAsync(new DeleteWishlistItemRequest(internalUserId, appId));
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

            var items = await _getWishlistUseCase.ExecuteAsync(new GetWishlistRequest(internalUserId));

            if (!items.Any())
            {
                return new ObjectResult(new { Count = 0 }) { StatusCode = 202 };
            }

            foreach (var item in items)
            {
                await _publishBackfillEventUseCase.ExecuteAsync(new PublishBackfillEventRequest(
                    internalUserId,
                    item.AppId,
                    item.DateAdded));
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
            await _setAlertRuleUseCase.ExecuteAsync(new SetAlertRuleRequest(internalUserId.ToString(), appId, thresholdAmount, currency));
            return Ok();
        }

        /// <summary>
        /// Proxy: deletes an alert rule (delegates to SteamTracker).
        /// </summary>
        [HttpDelete("{alertRuleId}/alert")]
        public async Task<ActionResult> DeleteAlertAsync(Guid alertRuleId)
        {
            int internalUserId = await _userContext.GetIdAsync();
            await _deleteAlertRuleUseCase.ExecuteAsync(new DeleteAlertRuleRequest(internalUserId.ToString(), alertRuleId));
            return Ok();
        }
    }
}
