using Application;
using Application.Contracts;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain.Exceptions;
using Infrastructure.SharedDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WishlistApi.Helpers;

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
        private readonly ISteamTrackerAlertProxy _alertProxy;

        public WishlistController(
            IUserContext userContext,
            IGetWishlistUseCase getWishlistUseCase,
            IAddWishlistItemUseCase addWishlistItemUseCase,
            IDeleteWishlistItemUseCase deleteWishlistItemUseCase,
            IGetWishlistStatsUseCase getWishlistStatsUseCase,
            IPublishBackfillEventUseCase publishBackfillEventUseCase,
            ISetAlertRuleUseCase setAlertRuleUseCase,
            IDeleteAlertRuleUseCase deleteAlertRuleUseCase,
            ISteamTrackerAlertProxy alertProxy)
        {
            _userContext = userContext;
            _getWishlistUseCase = getWishlistUseCase;
            _addWishlistItemUseCase = addWishlistItemUseCase;
            _deleteWishlistItemUseCase = deleteWishlistItemUseCase;
            _getWishlistStatsUseCase = getWishlistStatsUseCase;
            _publishBackfillEventUseCase = publishBackfillEventUseCase;
            _setAlertRuleUseCase = setAlertRuleUseCase;
            _deleteAlertRuleUseCase = deleteAlertRuleUseCase;
            _alertProxy = alertProxy;
        }

        [HttpGet()]
        public async Task<ActionResult<Wishlist>> GetWishlistAsync()// TODO add odata filtering to make DateAdded, AlertRuleId optional. In frontend search page, we don't need those fields.
        {
            int internalUserId = await _userContext.GetIdAsync();

            // Get local wishlist items
            var localItems = await _getWishlistUseCase.ExecuteAsync(new GetWishlistRequest(internalUserId));

            // Get alert rules from SteamTracker
            var alertRules = await _alertProxy.GetAlertRulesAsync(internalUserId.ToString());
            var alertMap = alertRules.ToDictionary(r => r.AppId, r => r.AlertRuleId);

            // Return core fields + alert info (price data is available via /api/prices)
            var result = localItems.Select(x => new WishlistItemDto(
                AppId: x.AppId,
                DateAdded: x.DateAdded,
                Name: x.AppName,
                AlertRuleId: alertMap.TryGetValue(x.AppId, out var alertId) ? alertId : null
            ));

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
