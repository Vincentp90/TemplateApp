using Application.Contracts;
using DataAccess;
using DataAccess.AppListings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Helpers;
using WishlistApi.DTOs;
using WishlistApi.HostedServices;

namespace Tests.IntegrationTests
{
    // TODO deal with statefullness
    // Possible good options: Respawn library, Bogus library
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
        public async Task Search_App_Test()
        {
            // Arrange
            var client = await _apiFactory.CreateAuthenticatedClientAsync();
            int appIdRandomOffset = Random.Shared.Next(0, Int32.MaxValue / 4);

            await _apiFactory.SeedAsync(async sp =>
            {
                var dbContext = sp.GetRequiredService<WishlistDbContext>();

                var appList = new List<AppListing>()
                {
                    new AppListing(){ appid = appIdRandomOffset + 1, name = "Dragon Quest" },
                    new AppListing(){ appid = appIdRandomOffset + 2, name = "Dragon Adventure" },
                    new AppListing(){ appid = appIdRandomOffset + 3, name = "Cat Quest" },
                    new AppListing(){ appid = appIdRandomOffset + 4, name = "filler1" },
                    new AppListing(){ appid = appIdRandomOffset + 5, name = "filler2" },
                    new AppListing(){ appid = appIdRandomOffset + 6, name = "filler3" },
                    new AppListing(){ appid = appIdRandomOffset + 7, name = "filler4" },
                    new AppListing(){ appid = appIdRandomOffset + 8, name = "filler5" },
                };

                dbContext.AppListings.AddRange(appList);
                await dbContext.SaveChangesAsync();
            });

            // Act
            var response = await client.GetAsync("/applistings/search/Dragon");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<List<AppListing>>();
            content.Should().NotBeNull();
            // Fuzzy dragon search should find the 2 dragon entries
            content.Count().Should().Be(2);
            content.Should().NotContain(x => x.name == "Cat Quest");
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

        //TODO uncomment once we have a way to reset DB
        /*
        [Fact]
        public async Task Auction_NoContent_WhenServerStartedWithoutAppsInDB_Test()
        {
            // Arrange
            var client = await _apiFactory.CreateAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/auctions/current");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }*/

        [Fact]
        public async Task AuctionTest()
        {
            // Arrange
            var client = await _apiFactory.CreateAuthenticatedClientAsync();

            int appIdRandomOffset = Random.Shared.Next(0, Int32.MaxValue / 4);
            await _apiFactory.SeedAsync(async sp =>
            {
                var dbContext = sp.GetRequiredService<WishlistDbContext>();

                var appList = new List<AppListing>()
                {
                    new AppListing(){ appid = appIdRandomOffset + 1, name = "App1" },
                    new AppListing(){ appid = appIdRandomOffset + 2, name = "App2" },
                    new AppListing(){ appid = appIdRandomOffset + 3, name = "App3" },
                };

                dbContext.AppListings.AddRange(appList);
                await dbContext.SaveChangesAsync();

                // Restart auction service so that it starts an auction with one of the apps
                // TODO once auctionservice is refactored, directly call StartNextAuctionAsync
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILogger<AuctionBackgroundService>>();
                var service = new AuctionBackgroundService(scopeFactory, logger);
                await service.StartAsync(CancellationToken.None);
                // Wait for auction service to initialize
                await Task.Delay(200);
            });

            // Act
            var response = await client.GetAsync("/auctions/current");

            // Assert
            response.EnsureSuccessStatusCode();
            var auction = await response.Content.ReadFromJsonAsync<AuctionDto>();
            auction.Should().NotBeNull();
            auction.AppName.Should().BeOneOf("App1", "App2", "App3");

            // Act
            var updatedAuction = auction with { CurrentPrice = 15.0m };
            response = await client.PostAsJsonAsync($"/auctions/current", updatedAuction);

            // Assert
            response.EnsureSuccessStatusCode();

            // Act
            response = await client.GetAsync("/auctions/current");

            // Assert
            var updatedAuctionResponse = await response.Content.ReadFromJsonAsync<AuctionDto>();
            updatedAuctionResponse.Should().NotBeNull();
            updatedAuctionResponse.CurrentPrice.Should().Be(15.0m);
            var rowVersionIncrement = updatedAuctionResponse.RowVersion - auction.RowVersion;
            rowVersionIncrement.Should().Be(1);
        }

        [Fact]
        public async Task GetWishlist_Test()
        {
            // Arrange
            var client = await _apiFactory.CreateAuthenticatedClientAsync();

            int appIdRandomOffset = Random.Shared.Next(0, Int32.MaxValue / 4);
            await _apiFactory.SeedAsync(async sp =>
            {
                var dbContext = sp.GetRequiredService<WishlistDbContext>();

                var appList = new List<AppListing>()
                {
                    new AppListing(){ appid = appIdRandomOffset + 1, name = "Game One" },
                    new AppListing(){ appid = appIdRandomOffset + 2, name = "Game Two" },
                    new AppListing(){ appid = appIdRandomOffset + 3, name = "Game Three" },
                };

                dbContext.AppListings.AddRange(appList);
                await dbContext.SaveChangesAsync();

                // Add items to wishlist
                await client.PostAsync($"/wishlist/{appIdRandomOffset + 1}", null);
                await client.PostAsync($"/wishlist/{appIdRandomOffset + 2}", null);
            });

            // Act
            var response = await client.GetAsync("/wishlist");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<WishlistDTOs.Wishlist>();
            content.Should().NotBeNull();
            content.Items.Count().Should().Be(2);
            content.Items.Select(x => x.AppId).Should().BeEquivalentTo(new[] { appIdRandomOffset + 1, appIdRandomOffset + 2 });

            // Act - test fields filtering
            response = await client.GetAsync("/wishlist?fields=appid,name");

            // Assert
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadFromJsonAsync<WishlistDTOs.Wishlist>();
            content.Should().NotBeNull();
            content.Items.Should().AllSatisfy(item =>
            {
                item.AppId.Should().NotBeNull();
                item.Name.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task AddWishlistItem_Test()
        {
            // Arrange
            var client = await _apiFactory.CreateAuthenticatedClientAsync();

            int appIdRandomOffset = Random.Shared.Next(0, Int32.MaxValue / 4);
            await _apiFactory.SeedAsync(async sp =>
            {
                var dbContext = sp.GetRequiredService<WishlistDbContext>();

                var appList = new List<AppListing>()
                {
                    new AppListing(){ appid = appIdRandomOffset + 1, name = "New Game" },
                };

                dbContext.AppListings.AddRange(appList);
                await dbContext.SaveChangesAsync();
            });

            // Act
            var response = await client.PostAsync($"/wishlist/{appIdRandomOffset + 1}", null);

            // Assert
            response.EnsureSuccessStatusCode();

            // Verify the item was added
            response = await client.GetAsync("/wishlist");
            var content = await response.Content.ReadFromJsonAsync<WishlistDTOs.Wishlist>();
            content.Should().NotBeNull();
            content.Items.Count().Should().Be(1);
            content.Items.First().AppId.Should().Be(appIdRandomOffset + 1);
        }

        [Fact]
        public async Task DeleteWishlistItem_Test()
        {
            // Arrange
            var client = await _apiFactory.CreateAuthenticatedClientAsync();

            int appIdRandomOffset = Random.Shared.Next(0, Int32.MaxValue / 4);
            await _apiFactory.SeedAsync(async sp =>
            {
                var dbContext = sp.GetRequiredService<WishlistDbContext>();

                var appList = new List<AppListing>()
                {
                    new AppListing(){ appid = appIdRandomOffset + 1, name = "ToRemove" },
                    new AppListing(){ appid = appIdRandomOffset + 2, name = "ToKeep" },
                };

                dbContext.AppListings.AddRange(appList);
                await dbContext.SaveChangesAsync();

                // Add both items to wishlist
                await client.PostAsync($"/wishlist/{appIdRandomOffset + 1}", null);
                await client.PostAsync($"/wishlist/{appIdRandomOffset + 2}", null);
            });

            // Act - delete one item
            var response = await client.DeleteAsync($"/wishlist/{appIdRandomOffset + 1}");

            // Assert
            response.EnsureSuccessStatusCode();

            // Verify the item was removed and the other is still there
            response = await client.GetAsync("/wishlist");
            var content = await response.Content.ReadFromJsonAsync<WishlistDTOs.Wishlist>();
            content.Should().NotBeNull();
            content.Items.Count().Should().Be(1);
            content.Items.First().AppId.Should().Be(appIdRandomOffset + 2);
        }
    }
}
