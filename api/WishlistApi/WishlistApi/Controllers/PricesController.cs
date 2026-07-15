using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WishlistApi.Controllers
{
    /// <summary>
    /// Passthrough endpoint for fetching game prices from SteamTracker.
    /// No authentication required — the frontend calls this directly.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PricesController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _steamTrackerUri;

        public PricesController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _steamTrackerUri = configuration.GetValue<string>("SteamTrackerUri");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GamePriceDto>>> GetPrices([FromQuery] int[] appIds)
        {
            if (string.IsNullOrEmpty(_steamTrackerUri) || appIds.Length == 0)
                return Ok(Array.Empty<GamePriceDto>());

            var query = string.Join("&", appIds.Select(a => $"appIds={a}"));
            var uri = $"{_steamTrackerUri}/api/games/prices?{query}";

            var httpClient = _httpClientFactory.CreateClient("SteamTracker");
            var response = await httpClient.GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var prices = JsonSerializer.Deserialize<IEnumerable<GamePriceDto>>(body, options);

            return Ok(prices ?? Array.Empty<GamePriceDto>());
        }
    }
}
