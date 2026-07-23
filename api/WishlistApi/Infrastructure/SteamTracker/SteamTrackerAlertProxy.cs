using Application.Contracts;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Infrastructure.SteamTracker;

/// <summary>
/// HttpClient-based proxy for SteamTracker's alert management endpoints.
/// Forwards alert creation and deletion to SteamTracker's API.
/// </summary>
public class SteamTrackerAlertProxy : ISteamTrackerAlertProxy
{
    private readonly HttpClient _httpClient;
    private readonly string? _baseUri;

    public SteamTrackerAlertProxy(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUri = configuration.GetValue<string>("SteamTrackerUri");
    }

    public async Task SetAlertRuleAsync(string userId, int appId, decimal thresholdAmount, string currency = "EUR")
    {
        if (string.IsNullOrEmpty(_baseUri))
            return;

        var uri = $"{_baseUri}/api/games/{appId}/alert"
            + $"?thresholdAmount={Uri.EscapeDataString(thresholdAmount.ToString("G", System.Globalization.CultureInfo.InvariantCulture))}"
            + $"&currency={Uri.EscapeDataString(currency)}";

        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("X-Internal-UserId", userId);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAlertRuleAsync(string userId, Guid alertRuleId)
    {
        if (string.IsNullOrEmpty(_baseUri))
            return;

        var uri = $"{_baseUri}/api/alert/{alertRuleId}";

        var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        request.Headers.Add("X-Internal-UserId", userId);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AlertRuleInfo>> GetAlertRulesAsync(string userId)
    {
        if (string.IsNullOrEmpty(_baseUri))
            return new List<AlertRuleInfo>();

        var uri = $"{_baseUri}/api/alerts";

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-Internal-UserId", userId);
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return new List<AlertRuleInfo>();

        var body = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rules = JsonSerializer.Deserialize<List<AlertRuleInfo>>(body, options);
        return rules ?? new List<AlertRuleInfo>();
    }
}
