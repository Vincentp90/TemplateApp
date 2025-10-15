using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class WishlistItemDateAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dateadded",
                table: "wishlist_items",
                type: "timestamp with time zone",
                nullable: false);
            migrationBuilder.Sql("UPDATE wishlist_items SET dateadded = NOW() WHERE dateadded IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dateadded",
                table: "wishlist_items");
        }
    }
}
