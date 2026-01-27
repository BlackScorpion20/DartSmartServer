using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartSmartNet.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMPRToPlayerStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tournaments_JoinCode",
                table: "Tournaments");

            migrationBuilder.AddColumn<decimal>(
                name: "AverageMPR",
                table: "player_stats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalCricketMarks",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_JoinCode",
                table: "Tournaments",
                column: "JoinCode",
                unique: true,
                filter: "\"JoinCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tournaments_JoinCode",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "AverageMPR",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalCricketMarks",
                table: "player_stats");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_JoinCode",
                table: "Tournaments",
                column: "JoinCode",
                unique: true,
                filter: "[JoinCode] IS NOT NULL");
        }
    }
}
