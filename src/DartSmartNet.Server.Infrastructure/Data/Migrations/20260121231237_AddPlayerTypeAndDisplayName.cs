using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartSmartNet.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerTypeAndDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "game_players",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "player_type",
                table: "game_players",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_name",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "player_type",
                table: "game_players");
        }
    }
}
