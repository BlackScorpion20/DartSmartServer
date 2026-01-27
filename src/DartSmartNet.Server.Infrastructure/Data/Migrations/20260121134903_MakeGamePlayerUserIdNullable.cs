using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartSmartNet.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeGamePlayerUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_players_game_id_user_id",
                table: "game_players");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "game_sessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "game_players",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_game_players_game_id_user_id",
                table: "game_players",
                columns: new[] { "game_id", "user_id" },
                unique: true,
                filter: "user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_players_game_id_user_id",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "game_sessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "game_players",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_players_game_id_user_id",
                table: "game_players",
                columns: new[] { "game_id", "user_id" },
                unique: true);
        }
    }
}
