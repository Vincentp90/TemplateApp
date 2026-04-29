using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace WishlistMauiApp.Services
{
    public interface IAuthService
    {
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetTokenAsync();
        Task SetTokenAsync(string token);
        Task<bool> LogoutAsync();
        Task LoginAsync(string username, string password);
    }

    public class AuthService : IAuthService
    {
        private const string TokenKey = "auth_token";
        private readonly HttpClient _httpClient;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // Optional: validate expiry if JWT
            return !IsExpired(token);
        }

        public Task<string?> GetTokenAsync()
            => SecureStorage.GetAsync(TokenKey);

        public Task SetTokenAsync(string token)
            => SecureStorage.SetAsync(TokenKey, token);

        public async Task<bool> LogoutAsync()
            => SecureStorage.Remove(TokenKey);

        public async Task LoginAsync(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", new { username, password });

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Incorrect email or password");
            }

            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed: {content}");
        }


        private bool IsExpired(string jwt)
        {
            // minimal check; don’t overcomplicate for now
            var parts = jwt.Split('.');
            if (parts.Length != 3) return false;

            var payload = parts[1]
                .Replace('-', '+').Replace('_', '/');
            var json = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(Pad(payload)));

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("exp", out var exp))
                return false;

            var expUnix = exp.GetInt64();
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return nowUnix >= expUnix;
        }

        private static string Pad(string s)
            => s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
    }
}
