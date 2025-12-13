using System.Collections.Generic;
using System.Xml;
using System;
using Microsoft.EntityFrameworkCore;
using DataAccess.AppListings;
using DataAccess.Wishlist;
using DataAccess.Users;
using DataAccess.Auctions;

namespace DataAccess
{
    public class WishlistDbContext : DbContext
    {
        public DbSet<AppListing> AppListings { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserDetails> UserDetails { get; set; }
        public DbSet<Auction> Auctions { get; set; }

        public WishlistDbContext(DbContextOptions<WishlistDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UUID)
                .IsUnique();

            modelBuilder.Entity<WishlistItem>()
                .HasIndex(wi => wi.UserID);

            modelBuilder.Entity<AppListing>()
                .HasIndex(a => a.appid);

            // Explains how to enable RowVersion with EF + Postgres:
            // https://github.com/npgsql/efcore.pg/issues/3270#issuecomment-2346017231
            modelBuilder.Entity<Auction>()
                .Property(e => e.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<UserDetails>()
                .Property(e => e.RowVersion)
                .IsRowVersion()
                .HasColumnName("xmin");
        }
    }
}
