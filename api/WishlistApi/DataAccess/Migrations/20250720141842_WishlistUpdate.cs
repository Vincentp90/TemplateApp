using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class WishlistUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* doesn't work
             * migrationBuilder.AlterColumn<int>(
                name: "appid",
                table: "wishlist_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");*/

            migrationBuilder.Sql(
                "ALTER TABLE wishlist_items ALTER COLUMN appid TYPE integer USING appid::integer;");

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_items_appid",
                table: "wishlist_items",
                column: "appid");

            migrationBuilder.AddForeignKey(
                name: "fk_wishlist_items_app_listings_appid",
                table: "wishlist_items",
                column: "appid",
                principalTable: "app_listings",
                principalColumn: "appid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_wishlist_items_app_listings_appid",
                table: "wishlist_items");

            migrationBuilder.DropIndex(
                name: "ix_wishlist_items_appid",
                table: "wishlist_items");

            migrationBuilder.AlterColumn<string>(
                name: "appid",
                table: "wishlist_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
