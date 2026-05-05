using DataAccess;
using DataAccess.AppListings;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tests.IntegrationTests
{
    // For doing load test on the applistings table (the apps we got from the steam API, so we can't test it with a Testcontainer)
    public class DatabaseTest
    {
        [Fact]
        public async Task CompareRandomStrategies()
        {
            // Dev DB docker should be running!
            var options = new DbContextOptionsBuilder<WishlistDbContext>().
                UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=example").
                UseSnakeCaseNamingConvention().
                Options;
            var _context = new WishlistDbContext(options);
            var da = new AppListingDA(_context);

            // warmup
            try
            {
                await da.GetRandomAppListingAsync();
            }
            catch (InvalidOperationException e) 
            {
                if (e.InnerException != null && e.InnerException.Message.Contains("Failed to connect"))
                    throw new Exception("Dev DB docker should be running!");
                else
                    throw;
            }

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
