using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AuctionAddedRV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE auctions DROP COLUMN row_version;");
            migrationBuilder.AddColumn<Guid>(
                name: "row_version",
                table: "auctions",
                type: "uuid",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_uuid()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "row_version",
                table: "auctions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldRowVersion: true,
                oldDefaultValueSql: "gen_random_uuid()");
        }
    }
}
