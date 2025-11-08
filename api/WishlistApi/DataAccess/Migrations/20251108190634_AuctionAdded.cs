using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AuctionAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auctions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    starting_price = table.Column<decimal>(type: "numeric", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    appid = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auctions", x => x.id);
                    table.ForeignKey(
                        name: "fk_auctions_app_listings_appid",
                        column: x => x.appid,
                        principalTable: "app_listings",
                        principalColumn: "appid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_auctions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_auctions_appid",
                table: "auctions",
                column: "appid");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_user_id",
                table: "auctions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auctions");
        }
    }
}
