using DataAccess;
using DataAccess.AppListings;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tests.Helpers;
using WishlistApi.DTOs;
using FluentAssertions;
using Renci.SshNet;

namespace Tests.IntegrationTests
{
    // TODO reorganize/split in smaller files. Tests of authorized methods in separate class
    public class ApiTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;
        private readonly ApiFactory _apiFactory;

        public ApiTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
            _apiFactory = factory;
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
            var userName = new Guid().ToString();
            var password = new Guid().ToString();

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

        [Fact]
        public async Task MeTest()
        {
            // Arrange
            var userAndCookie = await CreateNewUser();
            var secureClient = await GetSecureClient(userAndCookie);

            // Act
            var response = await secureClient.GetAsync("/auth/me");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(userAndCookie.Username, content.GetProperty("username").GetString());
        }

        [Fact]
        public async Task WishlistStatsTest()
        {
            // Arrange
            string OLDESTAPPNAME = "Old";

            var secureClient = await CreateNewAnonymousUserAndGetSecureClient();
            int appIdRandomOffset = Random.Shared.Next(0, Int32.MaxValue/4);
            await _apiFactory.SeedAsync(async sp =>
            {
                var dbContext = sp.GetRequiredService<WishlistDbContext>();

                var appList = new List<AppListing>()
                {
                    new AppListing(){ appid = appIdRandomOffset + 1, name = "Test App 1" },
                    new AppListing(){ appid = appIdRandomOffset + 2, name = "A whole lot of bbbbbbbbbbbbbbb" },
                    new AppListing(){ appid = appIdRandomOffset + 3, name = OLDESTAPPNAME },
                };

                dbContext.AppListings.AddRange(appList);
                await dbContext.SaveChangesAsync();
            });

            // Add some items to wishlist. Does this count as arrange or act?
            await secureClient.PostAsync($"/wishlist/{appIdRandomOffset + 3}", null);// Oldest item
            await secureClient.PostAsync($"/wishlist/{appIdRandomOffset + 2}", null);
            await secureClient.PostAsync($"/wishlist/{appIdRandomOffset + 1}", null);

            // Act
            var response = await secureClient.GetAsync("/wishlist/stats");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<WishlistDTOs.Stats>();
            content.Should().NotBeNull();
            content.AvgTimeAdded.Days.Should().Be(0);
            content.AvgTimeBetweenAdded.Days.Should().Be(0);
            content.OldestItem.Should().Be(OLDESTAPPNAME);
            content.MostCommonCharacter.Should().Be("b");
        }

        private class UserAndCookie
        {
            public required string Username { get; set; }
            public required string Password { get; set; }
            public required string Cookie { get; set; }
        }

        private async Task<HttpClient> CreateNewAnonymousUserAndGetSecureClient()
        {
            var userAndCookie = await CreateNewUser();
            var secureClient = await GetSecureClient(userAndCookie);
            return secureClient;
        }

        private async Task<UserAndCookie> CreateNewUser()
        {
            var userName = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();
            var login = new { Username = userName, Password = password };

            await _client.PostAsJsonAsync("/auth/register", login);

            var response = await _client.PostAsJsonAsync("/auth/login", login);            
            var authCookie = response.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("auth_token"));

            return new UserAndCookie { Username = userName, Password = password, Cookie = authCookie }; 
        }

        private async Task<HttpClient> GetSecureClient(UserAndCookie userAndCookie)
        {
            var client = _apiFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", userAndCookie.Cookie);
            return client;
        }
    }
}
