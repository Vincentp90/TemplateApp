using DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;

namespace Tests.Helpers
{
    public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()//TODO deprecated
            .WithDatabase("testdb")
            .WithUsername("user")
            .WithPassword("pass")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove hosted services (like SteamUpdaterService)
                var hostedServices = services
                    .Where(d => d.ServiceType == typeof(IHostedService))
                    .ToList();

                foreach (var svc in hostedServices)
                    services.Remove(svc);

                // Replace DB connection string
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<WishlistDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<WishlistDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()).UseSnakeCaseNamingConvention());
            });
        }

        public async Task InitializeAsync() => await _db.StartAsync();
        public async Task DisposeAsync() => await _db.DisposeAsync();//TODO warning

        public async Task SeedAsync(Func<IServiceProvider, Task> seed)
        {
            using var scope = Services.CreateScope();
            var sp = scope.ServiceProvider;

            await seed(sp);
        }
    }
}
