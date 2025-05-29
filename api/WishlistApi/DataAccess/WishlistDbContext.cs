using System.Collections.Generic;
using System.Xml;
using System;
using Microsoft.EntityFrameworkCore;
using DataAccess.AppListings;

namespace DataAccess
{
    public class WishlistDbContext : DbContext
    {
        public DbSet<AppListing> AppListings { get; set; }

        public WishlistDbContext(DbContextOptions<WishlistDbContext> options)
            : base(options) { }
    }
}
