using DataAccess;
using DataAccess.AppListings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Helpers;
using WishlistApi.DTOs;
using FluentAssertions;

namespace Tests.IntegrationTests
{
    public class ApiAuthorizedTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _apiFactory;

        public ApiAuthorizedTests(ApiFactory factory)
        {
            _apiFactory = factory;
        }

        [Fact]
        public async Task MeTest()
        {
            // Arrange
            var (client, username) = await _apiFactory.CreateAuthenticatedClientWithUserAsync();

            // Act
            var response = await client.GetAsync("/auth/me");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(username, content.GetProperty("username").GetString());
        }

        [Fact]
        public async Task WishlistStatsTest()
        {
            // Arrange
            string OLDESTAPPNAME = "Old";

            var client = await _apiFactory.CreateAuthenticatedClientAsync();
            // Random offset to make it less likely that unit tests interfere with each other, without manually setting different offsets in different unit tests. Not ideal way to do this. TODO better way?
            int appIdRandomOffset = Random.Shared.Next(0, Int32.MaxValue / 4);
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

            // Add apps to wishlist
            await client.PostAsync($"/wishlist/{appIdRandomOffset + 3}", null);// Oldest item
            await client.PostAsync($"/wishlist/{appIdRandomOffset + 2}", null);
            await client.PostAsync($"/wishlist/{appIdRandomOffset + 1}", null);

            // Act
            var response = await client.GetAsync("/wishlist/stats");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<WishlistDTOs.Stats>();
            content.Should().NotBeNull();
            content.AvgTimeAdded.Days.Should().Be(0);
            content.AvgTimeBetweenAdded.Days.Should().Be(0);
            content.OldestItem.Should().Be(OLDESTAPPNAME);
            content.MostCommonCharacter.Should().Be("b");
        }
    }
}
