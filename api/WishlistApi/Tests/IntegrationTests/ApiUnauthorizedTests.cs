using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Tests.Helpers;

namespace Tests.IntegrationTests
{
    public class ApiUnauthorizedTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public ApiUnauthorizedTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Post_register_returns_ok()
        {
            var request = new
            {
                Username = "testuser1",
                Password = "Test123!"
            };

            var response = await _client.PostAsJsonAsync("/auth/register", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithExistingUsername_ReturnsBadRequest()
        {
            var request = new
            {
                Username = "duplicateUser",
                Password = "Test123!"
            };

            // first call succeeds
            var first = await _client.PostAsJsonAsync("/auth/register", request);
            first.EnsureSuccessStatusCode();

            // second call should fail
            var second = await _client.PostAsJsonAsync("/auth/register", request);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

            var content = await second.Content.ReadAsStringAsync();
            Assert.Contains("Username already taken", content);
        }

        [Fact]
        public async Task LoginTest()
        {
            // Arrange
            var userName = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();

            var login = new { Username = userName, Password = password };
            await _client.PostAsJsonAsync("/auth/register", login);

            // Act
            var response = await _client.PostAsJsonAsync("/auth/login", login);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));

            var authCookie = cookies.FirstOrDefault(c => c.StartsWith("auth_token"));
            Assert.NotNull(authCookie);

            Assert.Contains("HttpOnly", authCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Secure", authCookie, StringComparison.OrdinalIgnoreCase);
        }
    }
}
