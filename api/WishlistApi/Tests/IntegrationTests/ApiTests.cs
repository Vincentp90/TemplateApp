using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Tests.Helpers;

namespace Tests.IntegrationTests
{
    public class ApiTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public ApiTests(ApiFactory factory)
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
    }
}
