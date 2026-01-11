using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBotSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BotSkillLevel",
                table: "players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBot",
                table: "players",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotSkillLevel",
                table: "players");

            migrationBuilder.DropColumn(
                name: "IsBot",
                table: "players");
        }
    }
}
