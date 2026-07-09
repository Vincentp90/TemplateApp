using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteamTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedPriceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_price_amount",
                table: "games");

            migrationBuilder.DropColumn(
                name: "current_price_currency",
                table: "games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal?>(
                name: "current_price_amount",
                table: "games",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "current_price_currency",
                table: "games",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }
    }
}
