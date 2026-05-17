using Application.Contracts;
using Application.Queries;
using DataAccess;
using DataAccess.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Tests.ApplicationTests
{
    public class UserQueriesTests
    {
        private WishlistDbContext _context;
        private UserQueries _queries;

        public UserQueriesTests()
        {
            var options = CreateNewContext();
            _context = new WishlistDbContext(options);
            _queries = new UserQueries(_context);
        }

        private static DbContextOptions<WishlistDbContext> CreateNewContext()
        {
            var builder = new DbContextOptionsBuilder<WishlistDbContext>();
            builder.UseInMemoryDatabase("UserQueriesTestDb_" + Guid.NewGuid())
                   .UseSnakeCaseNamingConvention();
            return builder.Options;
        }

        private async Task SeedUsers(int count)
        {
            var passwordHash = SHA256.Create().ComputeHash("password"u8.ToArray());
            var passwordSalt = new byte[32];
            new Random().NextBytes(passwordSalt);

            for (int i = 1; i <= count; i++)
            {
                var user = new User
                {
                    Username = $"user{i}@test.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };
                await _context.Users.AddAsync(user);
            }
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsCorrectNumberOfUsers_WhenFewerUsersThanLimit()
        {
            // Arrange - seed 3 users with limit of 10
            await SeedUsers(3);

            // Act
            var result = await _queries.GetUsersAsync(1, 10);

            // Assert - should return limit + 1 = 11 items, but only 3 exist
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsLimitPlusOne_WhenMoreUsersThanLimit()
        {
            // Arrange - seed 15 users with limit of 10
            await SeedUsers(15);

            // Act
            var result = await _queries.GetUsersAsync(1, 10);

            // Assert - should return limit + 1 = 11 items (overflow for next page check)
            result.Should().HaveCount(11);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsAllUsers_WhenExactlyLimitExist()
        {
            // Arrange - seed 10 users with limit of 10
            await SeedUsers(10);

            // Act
            var result = await _queries.GetUsersAsync(1, 10);

            // Assert - should return limit + 1 = 11 items, but only 10 exist
            result.Should().HaveCount(10);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsLimitedUsers_OnSecondPage()
        {
            // Arrange - seed 25 users with limit of 10
            await SeedUsers(25);

            // Act - page 2: skip 10, take 11
            var result = await _queries.GetUsersAsync(2, 10);

            // Assert - should return limit + 1 = 11 items (page 2 has users 11-21)
            result.Should().HaveCount(11);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsRemainingUsers_OnLastPage()
        {
            // Arrange - seed 25 users with limit of 10
            await SeedUsers(25);

            // Act - page 3: skip 20, take 11 (only 5 users remain)
            var result = await _queries.GetUsersAsync(3, 10);

            // Assert - should return remaining 5 users
            result.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetUsersAsync_DoesNotReturnNextPage_WhenExactlyOnPageBoundary()
        {
            // Arrange - seed exactly 20 users with limit of 10
            await SeedUsers(20);

            // Act - page 2: skip 10, take 11 (users 11-20 exist, that's 10)
            var result = await _queries.GetUsersAsync(2, 10);

            // Assert - should return exactly 10 (no overflow, last page)
            result.Should().HaveCount(10);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsPlusOne_WhenOneMoreUserExistsBeyondCurrentPage()
        {
            // Arrange - seed exactly 21 users with limit of 10
            await SeedUsers(21);

            // Act - page 2: skip 10, take 11 (users 11-21 exist, that's 11)
            var result = await _queries.GetUsersAsync(2, 10);

            // Assert - should return 11 (limit + 1), indicating there is a next page
            result.Should().HaveCount(11);
        }

        [Fact]
        public async Task GetUsersAsync_ClampsPageToMinimumOfOne()
        {
            // Arrange
            await SeedUsers(5);

            // Act - page 0 should be clamped to page 1
            var result = await _queries.GetUsersAsync(0, 10);
            result.Should().HaveCount(5);
        }


        [Fact]
        public async Task GetUsersAsync_ClampsLimitToMinimumOfOne()
        {
            // Arrange
            await SeedUsers(5);

            // Act - limit -1 should be clamped to 1
            var result = await _queries.GetUsersAsync(1, -1);

            // Assert - returns 1 + 1 overflow = 2 (not all 5 users)
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUsersAsync_ClampsLimitToMaximumOfTwoHundred()
        {
            // Arrange - seed 300 users
            await SeedUsers(300);

            // Act - limit 500 should be clamped to 200
            var result = await _queries.GetUsersAsync(1, 500);

            // Assert - should return 201 (200 + 1 overflow)
            result.Should().HaveCount(201);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsDtosWithCorrectFields()
        {
            // Arrange
            var expectedUuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            var expectedUsername = "testuser";

            await SeedUsers(0); // Clear DB

            var passwordHash = SHA256.Create().ComputeHash("password"u8.ToArray());
            var passwordSalt = new byte[32];
            var user = new User
            {
                Username = expectedUsername,
                UUID = expectedUuid,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _queries.GetUsersAsync(1, 10);

            // Assert
            result.Should().HaveCount(1);
            result[0].Uuid.Should().Be(expectedUuid);
            result[0].Username.Should().Be(expectedUsername);
        }

        [Fact]
        public async Task GetUsersAsync_OrdersUsersByUsername()
        {
            // Arrange - seed 5 users with limit of 10, should return all 5 ordered
            await SeedUsers(5);

            // Act
            var result = await _queries.GetUsersAsync(1, 5);

            // Assert - results should be ordered by username (user1 < user2 < ... < user5)
            var usernames = result.Select(u => u.Username).ToList();
            usernames.Should().BeInAscendingOrder();
        }
    }
}