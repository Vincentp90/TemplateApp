using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteamTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    alert_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    app_id = table.Column<int>(type: "integer", nullable: false),
                    trigger_below_price = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_rules", x => x.alert_rule_id);
                });

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    app_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    current_price = table.Column<string>(type: "text", nullable: true),
                    is_unavailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_games", x => x.app_id);
                });

            migrationBuilder.CreateTable(
                name: "tracked_games",
                columns: table => new
                {
                    app_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tracked_since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracked_games", x => x.app_id);
                });

            migrationBuilder.CreateTable(
                name: "price_snapshots",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<string>(type: "text", nullable: false),
                    discount_percent = table.Column<int>(type: "integer", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_snapshots", x => x.snapshot_id);
                    table.ForeignKey(
                        name: "fk_price_snapshots_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "app_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alert_rules_user_id_app_id",
                table: "alert_rules",
                columns: new[] { "user_id", "app_id" });

            migrationBuilder.CreateIndex(
                name: "ix_price_snapshots_game_id_captured_at",
                table: "price_snapshots",
                columns: new[] { "game_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tracked_games_app_id",
                table: "tracked_games",
                column: "app_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_rules");

            migrationBuilder.DropTable(
                name: "price_snapshots");

            migrationBuilder.DropTable(
                name: "tracked_games");

            migrationBuilder.DropTable(
                name: "games");
        }
    }
}
