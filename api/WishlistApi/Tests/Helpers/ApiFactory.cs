using Application;
using Application.Contracts;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.SharedDb;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Testcontainers.PostgreSql;

namespace Tests.Helpers
{
    public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18.1")
            .WithDatabase("testdb")
            .WithUsername("user")
            .WithPassword("pass")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Suppress EF Core migration/logging noise in tests
            builder.ConfigureLogging(logging =>
            {
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.None);
                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            });

            builder.ConfigureServices(services =>
            {
                // Remove hosted services (like SteamUpdaterService)
                var hostedServices = services
                    .Where(d => d.ServiceType == typeof(IHostedService))
                    .ToList();

                foreach (var svc in hostedServices)
                    services.Remove(svc);

                // Replace DB connection string
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<WishlistDbContext>));

                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<WishlistDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()).UseSnakeCaseNamingConvention());

                // Replace RabbitMQ with a no-op publisher so integration tests don't need a real broker
                var rmqFactoryDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IRabbitMqConnectionFactory));
                if (rmqFactoryDescriptor != null)
                    services.Remove(rmqFactoryDescriptor);

                services.AddSingleton<IRabbitMqConnectionFactory>(_ => new NoOpRabbitMqConnectionFactory());
                services.AddScoped<IEventPublisher>(_ => new NoOpRabbitMqEventPublisher());

                // Mock SteamTracker proxy for integration tests
                var alertProxyMock = new Mock<ISteamTrackerAlertProxy>();
                services.AddScoped(_ => alertProxyMock.Object);
            });
        }

        public async ValueTask InitializeAsync() => await _db.StartAsync();
        public override async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await base.DisposeAsync();
        }

        public async Task SeedAsync(Func<IServiceProvider, Task> seed)
        {
            using var scope = Services.CreateScope();
            var sp = scope.ServiceProvider;

            await seed(sp);
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            return (await CreateAuthenticatedClientWithUserAsync()).Client;
        }

        public async Task<(HttpClient Client, string Username)> CreateAuthenticatedClientWithUserAsync()
        {
            var client = this.CreateClient();

            var username = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();

            var credentials = new { Username = username, Password = password };

            await client.PostAsJsonAsync("/auth/register", credentials);

            var loginResponse = await client.PostAsJsonAsync("/auth/login", credentials);
            loginResponse.EnsureSuccessStatusCode();

            // Because dev/unit-test environment has no SSL, we need to manually set the cookie. HttpClient only sets secure=true cookies automaticly with HTTPS.
            var cookie = loginResponse.Headers
                .GetValues("Set-Cookie")
                .First(c => c.StartsWith("auth_token"));
            client.DefaultRequestHeaders.Add("Cookie", cookie);

            return (client, username);
        }
    }
}
