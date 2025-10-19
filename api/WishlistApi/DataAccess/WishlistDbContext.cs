using System.Collections.Generic;
using System.Xml;
using System;
using Microsoft.EntityFrameworkCore;
using DataAccess.AppListings;
using DataAccess.Wishlist;
using DataAccess.Users;

namespace DataAccess
{
    public class WishlistDbContext : DbContext
    {
        public DbSet<AppListing> AppListings { get; set; }

        public DbSet<WishlistItem> WishlistItems { get; set; }

        public DbSet<User> Users { get; set; }

        public WishlistDbContext(DbContextOptions<WishlistDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.username)
                .IsUnique();
        }
    }
}
