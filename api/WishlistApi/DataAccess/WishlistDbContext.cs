using System.Collections.Generic;
using System.Xml;
using System;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class WishlistDbContext : DbContext
    {
        public DbSet<GameListing> GameListings { get; set; }

        public WishlistDbContext(DbContextOptions<WishlistDbContext> options)
            : base(options) { }
    }
}
