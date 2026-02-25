using DataAccess;
using DataAccess.AppListings;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tests.IntegrationTests
{
    public class DatabaseTest
    {
        [Fact]
        public async Task CompareRandomStrategies()
        {
            var options = new DbContextOptionsBuilder<WishlistDbContext>().
                UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=example").
                UseSnakeCaseNamingConvention().
                Options;
            var _context = new WishlistDbContext(options);
            var da = new AppListingDA(_context);

            // warmup
            await da.GetRandomAppListingAsync();

            var sw = new Stopwatch();
            
            sw.Start();
            for (int i = 0; i < 50; i++)
                await da.GetRandomAppListingAsync();
            sw.Stop();

            var originalTime = sw.ElapsedMilliseconds;

            // Repeat for alternative method
            /*
            await da.GetRandomAppListingOldAsync();

            sw.Start();
            for (int i = 0; i < 50; i++)
                b = await da.GetRandomAppListingOldAsync();
            sw.Stop();

            var secondTime = sw.ElapsedMilliseconds;*/
        }
    }
}
